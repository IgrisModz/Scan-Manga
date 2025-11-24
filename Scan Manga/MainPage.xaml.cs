using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using MauiIcons.Core;
using MauiIcons.Material;
using Microsoft.Maui.Controls.Shapes;
using Scan_Manga.Pages;
using Scan_Manga.Services;
using System.Text.Json;

namespace Scan_Manga;

public partial class MainPage : ContentPage
{
    private readonly ISystemBarsService _systemBars;
    private HashSet<string> _visitedLinks = [];
    private string? _lastUrl;
    private bool _isFirstAppear = true;
    private bool _infoExpanded = false;

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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_lastUrl != null)
            Preferences.Set("LastUrl", _lastUrl);

        Preferences.Set("VisitedLinks", JsonSerializer.Serialize(_visitedLinks));
    }

    private async void OnRefresh(object sender, EventArgs e)
    {
        if (sender is VerticalStackLayout refreshBtn)
        {
            await refreshBtn.ScaleTo(0.85, 100, Easing.CubicInOut); // Rétrécit légèrement
            await refreshBtn.ScaleTo(1, 100, Easing.CubicInOut);    // Reviens à la taille normale
            var refreshLabel = refreshBtn.Children.OfType<Label>().First();
            refreshLabel.Rotation = 0;
            await refreshLabel.RotateTo(360, 500, Easing.CubicInOut);
            OnOverlayTapped(null, EventArgs.Empty);
        }

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
        _systemBars.SetLectureMode(isLecturePage);

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

    public static Task AnimateWidth(VisualElement view, double from, double to, uint length = 250)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        var animation = new Animation(v => view.WidthRequest = v, from, to, Easing.CubicInOut);
        animation.Commit(view, "WidthAnimation", 16, length, finished: (v, c) => taskCompletionSource.SetResult(true));

        return taskCompletionSource.Task;
    }

    public static Task AnimateHeight(VisualElement view, double from, double to, uint length = 250)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        var animation = new Animation(v => view.HeightRequest = v, from, to, Easing.CubicInOut);
        animation.Commit(view, "HeightAnimation", 16, length, finished: (v, c) => taskCompletionSource.SetResult(true));

        return taskCompletionSource.Task;
    }

    private async void OnExpandClicked(object? sender, EventArgs e)
    {
        double screenWidth = MainRoot.Width > 0 ? MainRoot.Width :
                             DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        double maxWidth = Math.Min(screenWidth - 20, 500);
        double minWidth = 50;

        if (!_infoExpanded)
        {
            ClickOutsideOverlay.IsVisible = true;

            // Rotation label
            var rotationTask = ExpandBtn.RotateTo(360, 500, Easing.CubicInOut);

            // Animate width et height
            await AnimateWidth(NavBar, NavBar.Width, maxWidth, 200);
            await AnimateHeight(NavBar, NavBar.Height, 80, 300);

            await rotationTask;

            // Change label Icon
            ExpandBtn.Icon(MaterialIcons.Close);

            // Afficher les boutons
            NavBarContent.IsVisible = true;
            await InfoBtn.FadeTo(1, 180);
            await RefreshBtn.FadeTo(1, 180);

            _infoExpanded = true;
        }
        else
        {
            ClickOutsideOverlay.IsVisible = false;

            // Masquer boutons
            await RefreshBtn.FadeTo(0, 140);
            await InfoBtn.FadeTo(0, 140);
            NavBarContent.IsVisible = false;

            // Revenir rotation
            var rotationTask = ExpandBtn.RotateTo(0, 450, Easing.CubicInOut);

            // Réduire width et height
            await AnimateHeight(NavBar, NavBar.Height, 50, 250);
            await AnimateWidth(NavBar, NavBar.Width, minWidth, 200);

            await rotationTask;

            // Revenir label Icon
            ExpandBtn.Icon(MaterialIcons.Notes);

            _infoExpanded = false;
        }
    }

    private async void OnInfoClicked(object sender, EventArgs e)
    {
        await InfoBtn.ScaleTo(0.85, 100, Easing.CubicInOut); // Rétrécit légèrement
        await InfoBtn.ScaleTo(1, 100, Easing.CubicInOut);    // Reviens à la taille normale

        OnOverlayTapped(null, EventArgs.Empty);

        var popupOptions = new PopupOptions
        {
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(20, 20, 20, 20),
                StrokeThickness = 0
            }
        };

        var popup = new InfoPopup();
        var popupResult = await this.ShowPopupAsync<ILegalPage>(popup, popupOptions);

        if (popupResult.WasDismissedByTappingOutsideOfPopup) return;

        await Navigation.PushAsync(popupResult.Result as Page);
    }

    private void OnOverlayTapped(object? sender, EventArgs e)
    {
        CloseInfoIfOpen();
    }

    private void OnOverlayPan(object sender, PanUpdatedEventArgs e)
    {
        CloseInfoIfOpen();
    }

    private void OnOverlayPinch(object sender, PinchGestureUpdatedEventArgs e)
    {
        CloseInfoIfOpen();
    }

    private void CloseInfoIfOpen()
    {
        if (_infoExpanded)
        {
            OnExpandClicked(null, EventArgs.Empty);
        }
    }
}
