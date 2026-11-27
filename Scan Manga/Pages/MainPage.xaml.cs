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
        MainWebView.HttpErrorOccurred += OnHttpErrorOccurred;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        BindingContext = new MainViewModel(MainWebView.ReloadPage);
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isNavigating = false;

        if (isFirstAppearance)
        {
            isFirstAppearance = false;
            ApplyUserTheme();

            var loadLast = settingsService.LoadLastUrlOnStartup();
            lastUrl = loadLast ? settingsService.GetLastUrl() : CustomWebView.DefaultUrl;

            MainWebView.Source = lastUrl;
        }

        visitedUrls = settingsService.IsHistoryEnabled() ? settingsService.GetVisitedUrls() : [];
        UpdateKeepScreenOn(lastUrl);

        if (lastUrl is not null && Window is not null)
        {
            fullScreenCts?.Cancel();
            var fullScreen = ShouldEnableFullScreen(lastUrl);
            Window.SetFullScreen(fullScreen);
            ApplySafeArea(fullScreen);
    }
    }

    protected override void OnDisappearing()
    {
        isNavigating = true;
        fullScreenCts?.Cancel();
        DeviceDisplay.KeepScreenOn = false;

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
        fullScreenCts?.Cancel();

        if (Handler?.MauiContext is null || lastUrl is null || isNavigating)
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

        using var cancellation = new CancellationTokenSource();
        fullScreenCts = cancellation;

        try
        {
            await Task.Delay(80, cancellation.Token).ConfigureAwait(true);

            if (!isNavigating && !cancellation.IsCancellationRequested)
            {
                Window?.EnableFullScreen();
                ApplySafeArea(true);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
    }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Full-screen transition error: {ex}");
        }
        finally
        {
            if (fullScreenCts == cancellation)
            {
                fullScreenCts = null;
            }
        }
    }

    #endregion

    #region WebView

	void OnHttpErrorOccurred(object? sender, WebViewErrorEventArgs e)
	{
		ErrorTitleLabel.Text = e.Title;
		ErrorMessageLabel.Text = e.Message;
	}

	void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
		if (e.NetworkAccess == NetworkAccess.Internet && MainWebView.HasError)
		{
			MainWebView.ReloadPage();
		}
	}

	async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
	{
			if (BindingContext is MainViewModel viewModel)
			{
				if (viewModel.IsRefreshing)
				{
					viewModel.IsRefreshing = false;
				}
			}

		try
		{
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

			if (!isNavigating)
			{
				fullScreenCts?.Cancel();
			var enableFullScreen = ShouldEnableFullScreen(e.Url);
			Window?.SetFullScreen(enableFullScreen);
			ApplySafeArea(enableFullScreen);
			UpdateKeepScreenOn(e.Url);
			}

			lastUrl = e.Url;
			settingsService.SetLastUrl(lastUrl);

			HandleHistory(e.Url);

			await InjectScriptWithVisitedLinksAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"WebView navigation error: {ex}");
		}
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

        var isAdBlockActive = settingsService.IsAdBlockerEnabled();
        var isHistoryActive = settingsService.IsHistoryEnabled();

        if (!isAdBlockActive && !isHistoryActive)
        {
            return;
        }

        try
        {
            var visitedJoined = JsonSerializer.Serialize(visitedUrls);

            using var stream = await FileSystem.OpenAppPackageFileAsync("adsRemover.js").ConfigureAwait(true);
            using var reader = new StreamReader(stream);
            var jsContent = await reader.ReadToEndAsync().ConfigureAwait(true);

            jsContent = jsContent.Replace("{isAdBlockEnabled}", isAdBlockActive.ToString().ToLower());
            jsContent = jsContent.Replace("{isHistoryEnabled}", isHistoryActive.ToString().ToLower());
            jsContent = jsContent.Replace("{visitedJoined}", visitedJoined);

            await MainWebView.EvaluateJavaScriptAsync(jsContent).ConfigureAwait(true);
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!isNavigating && !disposed)
            {
                UpdateKeepScreenOn(lastUrl);
            }
        });
    }

    void UpdateKeepScreenOn(string? url)
    {
        DeviceDisplay.KeepScreenOn = ShouldKeepScreenOn(url);
    }

    bool ShouldKeepScreenOn(string? url)
    {
        var mode = settingsService.GetKeepScreenOnMode();

        if (mode == KeepScreenOnMode.Disabled)
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

    bool ShouldEnableFullScreen(string url)
    {
        return settingsService.GetFullScreenMode() switch
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

    void OnHomeClicked(object? sender, EventArgs e) => MainWebView.Source = CustomWebView.DefaultUrl;

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                chargingService.ChargingStateChanged -= OnChargingChanged;
                MainWebView.Navigated -= OnWebViewNavigated;
                MainWebView.HttpErrorOccurred -= OnHttpErrorOccurred;
                Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;

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
