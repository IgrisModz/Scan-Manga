using Scan_Manga.Controls;
using Scan_Manga.Services;
using System.Text.Json;

#if NET9_0
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
#endif

namespace Scan_Manga;

public partial class MainPage : PageBase
{
    private HashSet<string> _visitedLinks = [];
    private string? _lastUrl;
    private bool _isFirstAppear = true;
    private CancellationTokenSource? _fullScreenCts;
    private bool _isNavigating;

    public MainPage(IFullScreenService fullScreenService) : base(fullScreenService)
    {
        InitializeComponent();
        BindingContext = this;

        // 🔑 Brancher Navigated
        MainWebView.Navigated += MainWebView_Navigated;
        MainWebView.HttpErrorOccurred += (s, e) =>
        {
            ErrorLabel.Text = $"{e.Code} : {e.Message}";
        };

        Connectivity.Current.ConnectivityChanged += (s, args) =>
        {
            if (args.NetworkAccess == NetworkAccess.Internet && MainWebView.HasError)
            {
                MainWebView.ReloadPage();
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstAppear)
        {
            _isFirstAppear = false;

            // 1. Appliquer le thème dès le démarrage
            ApplyUserTheme();

            // 2. Charger dernière URL selon préférence
            bool loadLast = Preferences.Get("LoadLastPageOnStartup", true);
            _lastUrl = loadLast
                ? Preferences.Get("LastUrl", "https://m.scan-manga.com/?home")
                : "https://m.scan-manga.com/?home";

            MainWebView.Source = _lastUrl;

            // 3. Charger l'historique
            var saved = Preferences.Get("VisitedLinks", string.Empty);
            _visitedLinks = string.IsNullOrEmpty(saved)
                ? []
                : JsonSerializer.Deserialize<HashSet<string>>(saved) ?? [];
        }
        else
        {
            _isNavigating = false;
            OnHandlerChanged();

            // Re-vérifier l'historique (au cas où il a été vidé dans les paramètres)
            var saved = Preferences.Get("VisitedLinks", string.Empty);
            if (string.IsNullOrEmpty(saved)) _visitedLinks.Clear();
        }
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext != null)
        {
            // Vérifier si on doit être en plein écran selon les réglages
            bool shouldBeFS = ShouldEnableFullScreen(_lastUrl ?? string.Empty);

            if (shouldBeFS)
            {
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
        }
    }

    protected override void OnDisappearing()
    {
        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        if (Preferences.Get("IsHistoryEnabled", true))
        {
            Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));
        }

        // Annule le plein écran quand on quitte la page
        _fullScreenCts?.Cancel();
        _isNavigating = true;

        base.OnDisappearing();
    }

    private async void MainWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        if (WebRefreshView.IsRefreshing) WebRefreshView.IsRefreshing = false;

        if (MainWebView.HasError)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(e.Url)) return;
        OfflineOverlay.IsVisible = false;

        bool enableFS = ShouldEnableFullScreen(e.Url);
        _fullScreenService.SetFullScreen(enableFS);
        ApplySafeArea(enableFS);

        bool userWantsKeepOn = Preferences.Get("KeepScreenOn", true);
        DeviceDisplay.KeepScreenOn = userWantsKeepOn && (IsLecturePage(e.Url) ?? false);

        _lastUrl = e.Url;
        Preferences.Set("LastUrl", _lastUrl);

        if (Preferences.Get("IsHistoryEnabled", true))
        {
            if (_visitedLinks.Add(e.Url))
            {
                if (_visitedLinks.Count > 2000)
                {
                    _visitedLinks = [.. _visitedLinks.Skip(_visitedLinks.Count - 2000)];
                }
                Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));
            }
        }

        await InjectScriptWithVisitedLinksAsync();
    }

    private async Task InjectScriptWithVisitedLinksAsync()
    {
        if (_lastUrl == null) return;
        
        bool isAdBlockActive = Preferences.Get("IsAdBlockerEnabled", true);
        bool isHistoryActive = Preferences.Get("IsHistoryEnabled", true);

        if (!isAdBlockActive && !isHistoryActive) return;

        try
        {
            string visitedJoined = JsonSerializer.Serialize(_visitedLinks);

            using var stream = await FileSystem.OpenAppPackageFileAsync("adsRemover.js");
            using var reader = new StreamReader(stream);
            var jsContent = await reader.ReadToEndAsync();

            // visitedJoined est un tableau JSON
            jsContent = jsContent.Replace("{isAdBlockEnabled}", isAdBlockActive.ToString().ToLower());
            jsContent = jsContent.Replace("{isHistoryEnabled}", isHistoryActive.ToString().ToLower());
            jsContent = jsContent.Replace("{visitedJoined}", visitedJoined);
            await MainWebView.EvaluateJavaScriptAsync(jsContent);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"JS Injection Error: {ex.Message}"); }
    }

    private void ApplySafeArea(bool isFullScreen)
    {
#if NET10_0_OR_GREATER
        SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
        MainRoot.SafeAreaEdges = isFullScreen ? SafeAreaEdges.None : SafeAreaEdges.Default;
#else
        On<iOS>().SetUseSafeArea(!isFullScreen);
#endif
    }

    private static void ApplyUserTheme()
    {
        var theme = Preferences.Get("AppTheme", "Système");
        Microsoft.Maui.Controls.Application.Current?.UserAppTheme = theme switch
        {
            "Clair" => AppTheme.Light,
            "Sombre" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    private static bool ShouldEnableFullScreen(string url)
    {
        var mode = Preferences.Get("FullScreenMode", "Lecture uniquement");

        return mode switch
        {
            "Toutes les pages" => true,
            "Lecture uniquement" => IsLecturePage(url) ?? false,
            _ => false // Désactivé
        };
    }

    private static bool? IsLecturePage(string? url) => url?.Contains("/lecture-en-ligne");
    
    private void OnRefresh(object? sender, EventArgs e) => MainWebView.ReloadPage();

    protected override bool OnBackButtonPressed()
    {
        if (MainWebView.CanGoBack) { MainWebView.GoBack(); return true; }
        return base.OnBackButtonPressed();
    }

    private void OnHomeTapped(object sender, TappedEventArgs e) => MainWebView.Source = "https://m.scan-manga.com/?home";
}
