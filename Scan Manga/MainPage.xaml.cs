using Scan_Manga.Controls;
using Scan_Manga.Services;
using System.Text.Json;
using Scan_Manga.ViewModels;

namespace Scan_Manga;

public partial class MainPage : PageBase
{
    private HashSet<string> _visitedLinks = [];
    private string? _lastUrl;
    private bool _isFirstAppear = true;
    private bool _isNavigating;
    private CancellationTokenSource? _fullScreenCts;
    private readonly IChargingService _chargingService;
    private readonly ISettingsService _settingsService;

    public MainPage(IFullScreenService fullScreenService, IChargingService chargingService, ISettingsService settingsService) : base(fullScreenService)
    {
        InitializeComponent();
        BindingContext = this;

        _chargingService = chargingService;
        _settingsService = settingsService;
        _chargingService.ChargingStateChanged += OnChargingChanged;

        MainWebView.Navigated += MainWebView_Navigated;
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
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstAppear)
        {
            _isFirstAppear = false;
            ApplyUserTheme();

            var loadLast = _settingsService.LoadLastPageOnStartup();
            _lastUrl = loadLast
                ? _settingsService.GetLastUrl(CustomWebView.DefaultUrl)
                : CustomWebView.DefaultUrl;

            MainWebView.Source = _lastUrl;

            _visitedLinks = _settingsService.GetVisitedLinks();
        }
        else
        {
            _isNavigating = false;
            OnHandlerChanged();

            _visitedLinks = _settingsService.GetVisitedLinks();
        }

        // 🔑 recalcul systématique KeepScreenOn
        UpdateKeepScreenOn(_lastUrl);
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler?.MauiContext == null || _lastUrl == null)
            return;

        var shouldBeFullScreen = ShouldEnableFullScreen(_lastUrl);

        if (!shouldBeFullScreen)
        {
            _fullScreenService?.ExitFullScreen();
            ApplySafeArea(false);
            return;
        }

        _fullScreenCts?.Cancel();
        _fullScreenCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(80, _fullScreenCts.Token);

            if (!_isNavigating && !_fullScreenCts.Token.IsCancellationRequested)
            {
                _fullScreenService?.EnterFullScreen();
                ApplySafeArea(true);
            }
        }
        catch (TaskCanceledException) { }
    }

    protected override void OnDisappearing()
    {
        UpdateKeepScreenOn(null);

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        if (Preferences.Get(nameof(SettingsViewModel.IsHistoryEnabled), true))
            Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));

        _fullScreenCts?.Cancel();
        _isNavigating = true;

        base.OnDisappearing();
    }

    #endregion

    #region WebView

    private async void MainWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        if (WebRefreshView.IsRefreshing)
            WebRefreshView.IsRefreshing = false;

        if (MainWebView.HasError)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(e.Url))
            return;

        OfflineOverlay.IsVisible = false;

        var enableFullScreen = ShouldEnableFullScreen(e.Url);
        _fullScreenService.SetFullScreen(enableFullScreen);
        ApplySafeArea(enableFullScreen);

        UpdateKeepScreenOn(e.Url);

        _lastUrl = e.Url;
        Preferences.Set("LastUrl", _lastUrl);

        HandleHistory(e.Url);

        await InjectScriptWithVisitedLinksAsync();
    }

    private void HandleHistory(string url)
    {
        if (!Preferences.Get(nameof(SettingsViewModel.IsHistoryEnabled), true))
            return;

        if (_visitedLinks.Add(url))
        {
            if (_visitedLinks.Count > 2000)
                _visitedLinks = [.. _visitedLinks.Skip(_visitedLinks.Count - 2000)];

            Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));
        }
    }

    #endregion

    #region JavaScript Injection

    private async Task InjectScriptWithVisitedLinksAsync()
    {
        if (_lastUrl == null)
            return;

        var isAdBlockActive = Preferences.Get(nameof(SettingsViewModel.IsAdBlockerEnabled), true);
        var isHistoryActive = Preferences.Get(nameof(SettingsViewModel.IsHistoryEnabled), true);

        if (!isAdBlockActive && !isHistoryActive)
            return;

        try
        {
            var visitedJoined = JsonSerializer.Serialize(_visitedLinks);

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

    private void OnChargingChanged(object? sender, bool isCharging)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateKeepScreenOn(_lastUrl));
    }

    private void UpdateKeepScreenOn(string? url)
    {
        DeviceDisplay.KeepScreenOn = ShouldKeepScreenOn(url);
    }

    private  bool ShouldKeepScreenOn(string? url)
    {
        var modeString = Preferences.Get(nameof(SettingsViewModel.SelectedKeepScreenOnMode), KeepScreenOnMode.Disabled.ToString());

        var result = Enum.TryParse<KeepScreenOnMode>(modeString, out var mode);
        if (!result || mode == KeepScreenOnMode.Disabled)
            return false;

        var isLecture = IsLecturePage(url) ?? false;

        return mode switch
        {
            KeepScreenOnMode.AllPages => true,
            KeepScreenOnMode.ReadingOnly => isLecture,
            KeepScreenOnMode.ChargingOnly => isLecture && _chargingService.IsCharging,
            _ => false
        };
    }

    private static bool ShouldEnableFullScreen(string url)
    {
        var modeString = Preferences.Get(nameof(SettingsViewModel.SelectedFullScreenMode), FullScreenMode.ReadingOnly.ToString());

        var result = Enum.TryParse<FullScreenMode>(modeString, out var mode);
        if (!result || mode == FullScreenMode.Disabled)
            return false;

        return mode switch
        {
            FullScreenMode.AllPages => true,
            FullScreenMode.ReadingOnly => IsLecturePage(url) ?? false,
            _ => false
        };
    }

    private static bool? IsLecturePage(string? url)
        => url?.Contains("/lecture-en-ligne");

    private void ApplyUserTheme()
    {
        var theme = _settingsService.GetTheme();
        Application.Current!.UserAppTheme = theme;
    }

    private void ApplySafeArea(bool isFullScreen)
    {
        SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
        MainRoot.SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
    }

    #endregion

    #region UI Events

    private void OnRefresh(object? sender, EventArgs e)
        => MainWebView.ReloadPage();

    protected override bool OnBackButtonPressed()
    {
        if (MainWebView.CanGoBack)
        {
            MainWebView.GoBack();
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private void OnHomeTapped(object sender, TappedEventArgs e)
        => MainWebView.Source = CustomWebView.DefaultUrl;

    #endregion
}
