using MauiFullScreen;
using Plugin.DeviceCharging;
using Scan_Manga.Controls;
using Scan_Manga.Services;
using Scan_Manga.ViewModels;
using System.Text.Json;

namespace Scan_Manga.Pages;

public partial class MainPage : PageBase, IDisposable
{
    readonly IChargingService chargingService;
    readonly ISettingsService settingsService;

    HashSet<string> visitedUrls = [];
    string? lastUrl;
    bool isFirstAppearance = true;
    bool isNavigating;
    CancellationTokenSource? fullScreenCts;
    bool disposed;

    public MainPage(IChargingService chargingService, ISettingsService settingsService)
    {
        this.chargingService = chargingService ?? throw new ArgumentNullException(nameof(chargingService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        InitializeComponent();

        this.chargingService.ChargingStateChanged += OnChargingChanged;
        MainWebView.Navigated += OnWebViewNavigated;
        MainWebView.HttpErrorOccurred += (s, e) =>
        {
            ErrorTitleLabel.Text = e.Title;
            ErrorMessageLabel.Text = e.Message;
        };

        Connectivity.Current.ConnectivityChanged += (s, args) =>
        {
            if (args.NetworkAccess == NetworkAccess.Internet && MainWebView.HasError)
            {
                MainWebView.ReloadPage();
            }
        };

        BindingContext = new MainViewModel(MainWebView.ReloadPage);
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (isFirstAppearance)
        {
            isFirstAppearance = false;
            ApplyUserTheme();

            var loadLast = settingsService.LoadLastUrlOnStartup();
            lastUrl = loadLast ? settingsService.GetLastUrl() : CustomWebView.DefaultUrl;

            MainWebView.Source = lastUrl;
        }
        else
        {
            isNavigating = false;
            // OnHandlerChanged();
        }

        visitedUrls = settingsService.IsHistoryEnabled() ? settingsService.GetVisitedUrls() : [];

        // 🔑 recalcul systématique KeepScreenOn
        UpdateKeepScreenOn(lastUrl);
    }

    protected override void OnDisappearing()
    {
        UpdateKeepScreenOn(null);

        if (lastUrl != null)
        {
            settingsService.SetLastUrl(lastUrl);
        }

        if (settingsService.IsHistoryEnabled())
        {
            settingsService.SetVisitedUrls(visitedUrls);
        }

        fullScreenCts?.Cancel();
        isNavigating = true;

        base.OnDisappearing();
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler?.MauiContext is null || lastUrl is null)
        {
            return;
        }

        var shouldBeFullScreen = ShouldEnableFullScreen(lastUrl);

        if (!shouldBeFullScreen)
        {
            Window?.DisableFullScreen();
            ApplySafeArea(false);
            return;
        }

        fullScreenCts?.Cancel();
        fullScreenCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(80, fullScreenCts.Token);

            if (!isNavigating && !fullScreenCts.Token.IsCancellationRequested)
            {
                Window?.EnableFullScreen();
                ApplySafeArea(true);
            }
        }
        catch (TaskCanceledException) { /* Ignore */ }
    }

    #endregion

    #region WebView

    async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        var ua = await MainWebView.EvaluateJavaScriptAsync("navigator.userAgent");
        System.Diagnostics.Debug.WriteLine(ua);

        if (BindingContext is MainViewModel viewModel)
        {
            if (viewModel.IsRefreshing)
            {
                viewModel.IsRefreshing = false;
            }
        }

        if (MainWebView.HasError)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(e.Url))
        {
            return;
        }

        OfflineOverlay.IsVisible = false;

        var enableFullScreen = ShouldEnableFullScreen(e.Url);
        Window?.SetFullScreen(enableFullScreen);
        ApplySafeArea(enableFullScreen);

        UpdateKeepScreenOn(e.Url);

        lastUrl = e.Url;
        settingsService.SetLastUrl(lastUrl);

        HandleHistory(e.Url);

        await InjectScriptWithVisitedLinksAsync();
    }

    void HandleHistory(string url)
    {
        if (!settingsService.IsHistoryEnabled())
        {
            return;
        }

        if (visitedUrls.Add(url))
        {
			if (visitedUrls.Count > 2000)
			{
				visitedUrls = [.. visitedUrls.Skip(visitedUrls.Count - 2000)];
			}

			settingsService.SetVisitedUrls(visitedUrls);
        }
    }

    #endregion

    #region JavaScript Injection

    async Task InjectScriptWithVisitedLinksAsync()
    {
        if (lastUrl == null)
        {
            return;
        }

        var isAdBlockActive = Preferences.Get(nameof(SettingsViewModel.IsAdBlockerEnabled), true);
        var isHistoryActive = Preferences.Get(nameof(SettingsViewModel.IsHistoryEnabled), true);

        if (!isAdBlockActive && !isHistoryActive)
        {
            return;
        }

        try
        {
            var visitedJoined = JsonSerializer.Serialize(visitedUrls);

            using var stream = await FileSystem.OpenAppPackageFileAsync("adsRemover.js");
            using var reader = new StreamReader(stream);
            var jsContent = await reader.ReadToEndAsync();

            jsContent = jsContent.Replace("{isAdBlockEnabled}", isAdBlockActive.ToString().ToLower());
            jsContent = jsContent.Replace("{isHistoryEnabled}", isHistoryActive.ToString().ToLower());
            jsContent = jsContent.Replace("{visitedJoined}", visitedJoined);

            await MainWebView.EvaluateJavaScriptAsync(jsContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"JS Injection Error: {ex.Message}");
        }
    }

    #endregion

    #region Helpers

    void OnChargingChanged(object? _, bool isCharging)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateKeepScreenOn(lastUrl));
    }

    void UpdateKeepScreenOn(string? url)
    {
        DeviceDisplay.KeepScreenOn = ShouldKeepScreenOn(url);
    }

    bool ShouldKeepScreenOn(string? url)
    {
        var modeString = Preferences.Get(nameof(SettingsViewModel.SelectedKeepScreenOnMode), KeepScreenOnMode.Disabled.ToString());

        var result = Enum.TryParse<KeepScreenOnMode>(modeString, out var mode);

        if (!result || mode == KeepScreenOnMode.Disabled)
        {
            return false;
        }

        var isLecture = IsLecturePage(url) ?? false;

        return mode switch
        {
            KeepScreenOnMode.AllPages => true,
            KeepScreenOnMode.ReadingOnly => isLecture,
            KeepScreenOnMode.ChargingOnly => isLecture && chargingService.IsCharging,
            _ => false
        };
    }

    static bool ShouldEnableFullScreen(string url)
    {
        var modeString = Preferences.Get(nameof(SettingsViewModel.SelectedFullScreenMode), FullScreenMode.ReadingOnly.ToString());

        var result = Enum.TryParse<FullScreenMode>(modeString, out var mode);
        return result && mode != FullScreenMode.Disabled && mode switch
        {
            FullScreenMode.AllPages => true,
            FullScreenMode.ReadingOnly => IsLecturePage(url) ?? false,
            _ => false
        };
    }

    static bool? IsLecturePage(string? url) => url?.Contains("/lecture-en-ligne");

    void ApplyUserTheme()
    {
        var theme = settingsService.GetAppTheme();
        Application.Current!.UserAppTheme = theme;
    }

    void ApplySafeArea(bool isFullScreen)
    {
        SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
        MainRoot.SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
    }

    #endregion

    #region UI Events

    void OnRefresh(object? sender, EventArgs e) => MainWebView.ReloadPage();

    protected override bool OnBackButtonPressed()
    {
        if (MainWebView.CanGoBack)
        {
            MainWebView.GoBack();
            return true;
        }
        return base.OnBackButtonPressed();
    }

    void OnHomeClicked(object sender, EventArgs e) => MainWebView.Source = CustomWebView.DefaultUrl;

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                chargingService.ChargingStateChanged -= OnChargingChanged;
                MainWebView.Navigated -= OnWebViewNavigated;

                fullScreenCts?.Cancel();
                fullScreenCts?.Dispose();
                fullScreenCts = null;
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
