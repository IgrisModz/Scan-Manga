using Scan_Manga.Services;

namespace Scan_Manga;

public partial class MainPage : ContentPage
{
    private readonly ISystemBarsService _systemBars;
    private readonly HashSet<string> _visitedUrls = [];
    private string? _lastUrl;

    public MainPage(ISystemBarsService systemBars)
    {
        InitializeComponent();
        _systemBars = systemBars;

        MainWebView.Navigated += MainWebView_Navigated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Charger dernière URL
        _lastUrl = Preferences.Get("LastUrl", "https://www.scan-manga.com/?home");
        MainWebView.Source = _lastUrl;

        // Charger URLs déjà visitées
        var savedVisited = Preferences.Get("VisitedUrls", "");
        if (!string.IsNullOrEmpty(savedVisited))
            _visitedUrls.UnionWith(savedVisited.Split(';', StringSplitOptions.RemoveEmptyEntries));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        Preferences.Set("VisitedUrls", string.Join(";", _visitedUrls));
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

    private void MainWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        // Arrêter le refresh si actif
        if (WebRefreshView.IsRefreshing)
            WebRefreshView.IsRefreshing = false;

        if (e.Result == WebNavigationResult.Failure)
        {
            OfflineOverlay.IsVisible = true;
            return;
        }

        if (!string.IsNullOrEmpty(e.Url) && e.Result == WebNavigationResult.Success)
        {
            OfflineOverlay.IsVisible = false;

            bool isLecturePage = e.Url.Contains("/lecture-en-ligne");
            _systemBars.SetLectureMode(isLecturePage);

            // Sauvegarder en mémoire
            _lastUrl = e.Url;

            if (_lastUrl != null)
                Preferences.Set("LastUrl", _lastUrl);

            if (!_visitedUrls.Add(e.Url))
            {
                // ✅ Limiter à 2000 liens max
                if (_visitedUrls.Count > 2000)
                {
                    var toKeep = _visitedUrls.Skip(_visitedUrls.Count - 2000).ToList();
                    _visitedUrls.Clear();
                    foreach (var url in toKeep) _visitedUrls.Add(url);
                }
            }

            Preferences.Set("VisitedUrls", string.Join(";", _visitedUrls));

            // Recolorer les liens visités
            if (MainWebView.Handler?.PlatformView is Android.Webkit.WebView webView)
            {
                string visitedJoined = string.Join("','", _visitedUrls);

                string jsCode = $@"
                (function() {{
                    var visited = ['{visitedJoined}'];
                    var anchors = document.querySelectorAll('a');
                    anchors.forEach(function(a) {{
                        var isTargeted = 
                            a.closest('div.chapitre_nom') ||
                            a.classList.contains('telecharger') ||
                            a.classList.contains('lecture_en_ligne') ||
                            a.classList.contains('telechargement') ||
                            a.classList.contains('lel_tchapt') ||
                            a.classList.contains('lecture_online');
                        if (visited.includes(a.href) && isTargeted) {{
                            a.style.color = '#e4adaa';
                        }}
                    }});
                }})();";

                webView.EvaluateJavascript(jsCode, null);
            }
        }
    }
}
