using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Extensions;
using Scan_Manga.Controls;
using Scan_Manga.Services;
using System.Runtime.Versioning;
using System.Text.Json;
using static Microsoft.Maui.Controls.Application;

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

    [SupportedOSPlatform("android23.0")]
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
            OnHandlerChanged();
            var color = (Color)Current!.Resources["Primary"];
            StatusBar.SetColor(color);
        }
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext != null)
        {
            if (_lastUrl?.Contains("/lecture-en-ligne") ?? false)
            {
                await Task.Delay(80);
                _fullScreenService?.EnterFullScreen();
#if NET10_0_OR_GREATER
                SafeAreaEdges = SafeAreaEdges.None;
                MainRoot.SafeAreaEdges = SafeAreaEdges.None;
#else
                On<iOS>().SetUseSafeArea(true);
#endif

            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));
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

        bool isLecturePage = e.Url.Contains("/lecture-en-ligne");
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

        // Recolorer les liens visités
        string visitedJoined = JsonSerializer.Serialize(_visitedLinks);

        string js = $@"
            (function() {{
                const selectors = [
                    'div.BDPFGA[data-type=""_mgwidget""]',
                    'div.PUBFUTURE',
                    'div[data-unit]',
                    'div#teads_inread',
                    'script[src*=""richardghain.com""]',
                    'script[src*=""adschill.com""]',
                    'script[src*=""acscdn.com""]'
                ];
                // --- 1. Suppression des publicités ---
                function removeAds() {{
                    
                    // Supprime toutes les classes connues
                    selectors.forEach(sel => {{
                        document.querySelectorAll(sel).forEach(el => el.remove());
                    }});

                    const container = document.querySelector('.reader_container');

                    while (container.firstElementChild) {{
                        const first = container.firstElementChild;
                    
                        // Vérifie si c'est bien un <div> avec la classe ""reader_view""
                        if (first.tagName.toLowerCase() === 'div' && first.classList.contains('reader_view')) {{
                            // On arrête la boucle car le premier enfant correspond
                            break;
                        }} else {{
                            // Sinon on supprime le premier enfant
                            container.removeChild(first);
                        }}
                    }}

                    // Récupère la balise <html>
                    const html = document.documentElement;
                    
                    // Parcourt tous les enfants directs de <html>
                    Array.from(html.children).forEach(child => {{
                        if (child.tagName.toLowerCase() !== 'head' && child.tagName.toLowerCase() !== 'body') {{
                            html.removeChild(child);
                        }}
                    }});

                    // Supprime les balises in-page-message
                    document.querySelectorAll('in-page-message, iframe').forEach(e => {{
                        if (e.shadowRoot) e.shadowRoot.innerHTML = '';
                        e.remove();
                    }});
                }}

                removeAds();
                const adObserver = new MutationObserver(removeAds);
                adObserver.observe(document.body, {{ childList: true, subtree: true }});
                //setInterval(removeAds, 500);

                // --- 2. Mise en couleur des chapitres visités ---

                // Injection CSS ciblé
                const style = document.createElement('style');
                style.textContent = `
                    span.i a.visited,
                    a.l_read.visited,
                    div.top a.atop.visited {{
                        color: #e0a19d !important;
                    }}
                `;
                document.head.appendChild(style);

                var visited = {visitedJoined};
                var anchors = document.querySelectorAll('span.i a, a.l_read, div.top a.atop');
                anchors.forEach(function(link) {{
                    if (visited.includes(link.href)) {{
                        link.classList.add('visited');
                    }}
                }});
            }})();";

        await MainWebView.EvaluateJavaScriptAsync(js);
    }

    private async void OnInfoClicked(object sender, EventArgs e)
    {
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

        if (popupResult.WasDismissedByTappingOutsideOfPopup)
            return;

        if (popupResult.Result == null)
            return;

        await Shell.Current.GoToAsync(popupResult.Result);
    }
}
