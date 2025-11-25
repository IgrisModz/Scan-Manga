using Scan_Manga.Controls;
using Scan_Manga.Pages;
using Scan_Manga.Services;
using System.Text.Json;

namespace Scan_Manga;

public partial class MainPage : ContentPage
{
    private readonly ISystemBarsService? _systemBars;
    private HashSet<string> _visitedLinks = [];
    private string? _lastUrl;
    private bool _isFirstAppear = true;

    public MainPage(ISystemBarsService systemBars)
    {
        InitializeComponent();
        _systemBars = systemBars;

        // 🔑 Brancher Navigated
        MainWebView.Navigated += MainWebView_Navigated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstAppear)
        {
            _isFirstAppear = false;
            // Charger dernière URL
            _lastUrl = Preferences.Get("LastUrl", "https://www.scan-manga.com/?home");
            MainWebView.Source = _lastUrl;

            // Charger URLs déjà visitées

            var saved = Preferences.Get("VisitedLinks", string.Empty);
            _visitedLinks = string.IsNullOrEmpty(saved)
                ? []
                : JsonSerializer.Deserialize<HashSet<string>>(saved) ?? [];
        }
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext != null)
        {
            if (_lastUrl?.Contains("/lecture-en-ligne") ?? false)
            {
                await Task.Delay(50);
                _systemBars?.SetLectureMode(true);
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

    private void OnRefresh(object sender, EventArgs e)
    {
        MainWebView.Reload();
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

        if (e.Result == WebNavigationResult.Failure)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(e.Url) || e.Result != WebNavigationResult.Success) return;
        
        OfflineOverlay.IsVisible = false;

        bool isLecturePage = e.Url.Contains("/lecture-en-ligne");
        _systemBars?.SetLectureMode(isLecturePage);

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
                    'div.BDPFGA[id^=""pf-""]',
                    'div.PUBFUTURE[id^=""pf-""]',
                    'div[data-unit]',
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

                    // Supprime les balises in-page-message
                    document.querySelectorAll('in-page-message').forEach(e => {{
                        if (e.shadowRoot) e.shadowRoot.innerHTML = '';
                        e.remove();
                    }});

                    // Supprime les iframes de type publicitaire
                    document.querySelectorAll('iframe').forEach(iframe => {{
                        const src = iframe.src || '';
                        if (src.includes('crcdn.org') || src.includes('adexchangeclear') || iframe.title === 'offer') {{
                            iframe.remove();
                        }}
                    }});
                }}

                removeAds();
                const adObserver = new MutationObserver(removeAds);
                adObserver.observe(document.body, {{ childList: true, subtree: true }});
                // setInterval(removeAds, 500);

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
        var popup = new DynamicPopup<ILegalPage>();

        popup.SetContent(
            "Informations",
            new Dictionary<string, ILegalPage>
            {
        { "Mentions légales", new LegalNoticesPage() },
        { "Politique de confidentialité", new PrivacyPolicyPage() },
        { "Conditions générales d’utilisation", new TermsOfUsePage() },
        { "À propos", new AboutPage() }
            }
        );

        var result = await popup.ShowAsync(this);

        if (result.WasDismissedByTappingOutsideOfPopup)
            return;

        if (result != null)
        {
            // 🔑 mémoriser si le mode lecture est actif avant de naviguer
            bool lectureModeBeforeNavigation = _lastUrl?.Contains("/lecture-en-ligne") ?? false;

            // Naviguer vers la page externe
            await Navigation.PushAsync(result.Value as ContentPage);

            // Désactiver le mode lecture seulement si on était en lecture avant
            if (lectureModeBeforeNavigation)
            {
                _systemBars?.SetLectureMode(false);

                // Réactiver le mode lecture uniquement au retour de cette page
                void OnPageDisappearing(object? s, EventArgs args)
                {
                    _systemBars?.SetLectureMode(true);
                    ((ContentPage)result.Value).Disappearing -= OnPageDisappearing;
                }

                ((ContentPage)result.Value).Disappearing += OnPageDisappearing;
            }
        }
    }
}
