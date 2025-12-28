using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Extensions;
using Scan_Manga.Controls;
using Scan_Manga.Services;
using System.Text.Json;

#if NET9_0
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
#endif

namespace Scan_Manga;

public partial class MainPage : ContentPage
{
    private readonly IFullScreenService _fullScreenService;
    private HashSet<string> _visitedLinks = [];
    private string? _lastUrl;
    private bool _isFirstAppear = true;
    private CancellationTokenSource? _fullScreenCts;
    private bool _isNavigating;

    public MainPage(IFullScreenService fullScreenService)
    {
        InitializeComponent();
        BindingContext = this;

        _fullScreenService = fullScreenService;

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
                OnRefresh(null, EventArgs.Empty);
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstAppear)
        {
            _isFirstAppear = false;
            // Charger dernière URL
            _lastUrl = Preferences.Get("LastUrl", "https://m.scan-manga.com/?home");
            MainWebView.Source = _lastUrl;

            // Charger URLs déjà visitées

            var saved = Preferences.Get("VisitedLinks", string.Empty);
            _visitedLinks = string.IsNullOrEmpty(saved)
                ? []
                : JsonSerializer.Deserialize<HashSet<string>>(saved) ?? [];
        }
        else
        {
            _isNavigating = false; // On revient sur la page
            OnHandlerChanged();
        }
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext != null)
        {
            if (IsLecturePage(_lastUrl) ?? false)
            {
                // Annule toute opération précédente
                _fullScreenCts?.Cancel();
                _fullScreenCts = new CancellationTokenSource();

                try
                {
                    await Task.Delay(80, _fullScreenCts.Token);

                    if (!_isNavigating && !_fullScreenCts.Token.IsCancellationRequested)
                    {
                        _fullScreenService?.EnterFullScreen();
#if NET10_0_OR_GREATER
                SafeAreaEdges = SafeAreaEdges.None;
                MainRoot.SafeAreaEdges = SafeAreaEdges.None;
#else
                    On<iOS>().SetUseSafeArea(true);
#endif
                        var color = (Color)Microsoft.Maui.Controls.Application.Current!.Resources["Primary"];
                        if (OperatingSystem.IsAndroidVersionAtLeast(23) ||
                            OperatingSystem.IsIOSVersionAtLeast(15))
                            StatusBar.SetColor(color);
                    }
                }
                catch (TaskCanceledException) { }

            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));

        // Annule le plein écran quand on quitte la page
        _fullScreenCts?.Cancel();
        _isNavigating = true;
    }

    private void OnRefresh(object? sender, EventArgs e)
    {
        MainWebView.ReloadPage();
    }

    protected override bool OnBackButtonPressed()
    {
        if (MainWebView.CanGoBack)
        {
            MainWebView.GoBack();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async void MainWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        // Arrêter le refresh si actif
        if (WebRefreshView.IsRefreshing)
            WebRefreshView.IsRefreshing = false;

        if (MainWebView.HasError)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(e.Url)) return;
        
        OfflineOverlay.IsVisible = false;

        bool isLecturePage = IsLecturePage(e.Url) ?? false;
        _fullScreenService.SetFullScreen(isLecturePage);
#if NET10_0_OR_GREATER
        SafeAreaEdges = isLecturePage ? SafeAreaEdges.None : SafeAreaEdges.Default;
        MainRoot.SafeAreaEdges = isLecturePage ? SafeAreaEdges.None : SafeAreaEdges.Default;
#else
        On<iOS>().SetUseSafeArea(isLecturePage);
#endif

        // Sauvegarder en mémoire
        _lastUrl = e.Url;

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        if (!_visitedLinks.Add(e.Url))
        {
            // ✅ Limiter à 2000 liens max
            if (_visitedLinks.Count > 2000)
            {
                var toKeep = _visitedLinks.Skip(_visitedLinks.Count - 2000).ToList();
                _visitedLinks.Clear();
                foreach (var url in toKeep) _visitedLinks.Add(url);
            }
        }

        Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));

        await InjectScriptWithVisitedLinksAsync();
    }

    private void OnHomeClicked(object sender, EventArgs e) => MainWebView.Source = "https://m.scan-manga.com/?home";

    private async void OnInfoClicked(object sender, EventArgs e)
    {
        // Marque qu'on est en train de naviguer
        _isNavigating = true;
        // Annule immédiatement toute tentative de plein écran
        _fullScreenCts?.Cancel();

        var infoPopup = new InfoPopup();

        var popupOptions = new PopupOptions
        {
            Shadow = new Shadow
            {
                Brush = Brush.White,
                Offset = new Point(0, 2),
                Opacity = 0.8f,
                Radius = 8
            },
        };
        var popupResult = await this.ShowPopupAsync<string>(infoPopup, popupOptions);
        
        // ShowPopup désactive le plein écran donc il faut le réinitialiser manuellement
        _fullScreenService.IsFullScreen = false;

        if (popupResult.WasDismissedByTappingOutsideOfPopup ||
            popupResult.Result == null)
        {
            _isNavigating = false; // Annulé, on reste
            return;
        }

        await Shell.Current.GoToAsync(popupResult.Result);
    }

    private void OnHomeClicked(object sender, EventArgs e)
    {
        MainWebView.Source = "https://m.scan-manga.com/?home";
    }

    private async Task InjectScriptWithVisitedLinksAsync()
    {
        if (_lastUrl == null) return;

        string visitedJoined = JsonSerializer.Serialize(_visitedLinks);

        using var stream = await FileSystem.OpenAppPackageFileAsync("adsRemover.js");
        using var reader = new StreamReader(stream);
        var jsTemplate = await reader.ReadToEndAsync();

        // visitedJoined est un tableau JSON
        string jsContent = jsTemplate.Replace("{visitedJoined}", visitedJoined);
        await MainWebView.EvaluateJavaScriptAsync(jsContent);
    }

    private static bool? IsLecturePage(string? url)
    {
        return url?.Contains("/lecture-en-ligne");
    }
}
