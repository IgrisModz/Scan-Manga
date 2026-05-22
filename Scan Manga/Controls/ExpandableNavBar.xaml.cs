using MauiIcons.Core.Extensions;
using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Helpers;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid
{
    public event EventHandler? RefreshClicked;
    public event EventHandler? HomeClicked;

    private bool _navBarExpanded = false;
    private bool _isVerticalExpanded = false;

    private const double MinWidth = 50;
    private const double MinHeight = 50;
    private const double ExpandedHeight = 210;

    public ExpandableNavBar()
    {
        InitializeComponent();

        ExpandBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OnExpandClicked())
        });

        SafeAreaEdges = SafeAreaEdges.None;
    }

    private async Task OnExpandClicked()
    {
        var screenWidth = Width > 0 ? Width :
                             DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        var maxWidth = Math.Min(screenWidth - 20, 400);

        if (!_navBarExpanded)
        {
            ClickOutsideOverlay.IsVisible = true;

            var expandedTask = ExpandBtn.RotateToSafe(360, 500);

            await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, maxWidth, 200);
            await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, 110, 300);

            await expandedTask;

            ExpandBtn.Text = MaterialSymbolsRoundedIcons.Close.GetGlyph();
            ExpandBtnContainer.IsVisible = false;
            NavBarContent.IsVisible = true;
            MoreBtnContainer.IsVisible = true;

            _navBarExpanded = true;
        }
        else
        {
            await CloseNavBarInternal();
        }
    }

    private async void OnMoreTapped(object? sender, TappedEventArgs e)
    {
        if (!_isVerticalExpanded)
        {
            MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowDown.GetGlyph();

            await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, ExpandedHeight, 250);

            ExtraOptionsContainer.IsVisible = true;
            await ExtraOptionsContainer.FadeToSafe(1, 200);
            _isVerticalExpanded = true;
        }
        else
        {
            await CloseVerticalMenu();
        }
    }

    private async Task CloseVerticalMenu()
    {
        if (!_isVerticalExpanded) return;

        MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph(); // Retour icône haut

        await ExtraOptionsContainer.FadeToSafe(0, 150);
        ExtraOptionsContainer.IsVisible = false;

        await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, 110, 200);

        _isVerticalExpanded = false;
    }

    private async Task CloseNavBarInternal()
    {
        ClickOutsideOverlay.IsVisible = false;

        // Si le menu vertical est ouvert, on le ferme d'abord
        if (_isVerticalExpanded)
        {
            MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph(); // Retour icône haut

            await ExtraOptionsContainer.FadeToSafe(0, 150);
            ExtraOptionsContainer.IsVisible = false;

            _isVerticalExpanded = false;
        }

        NavBarContent.IsVisible = false;
        MoreBtnContainer.IsVisible = false;
        ExpandBtnContainer.IsVisible = true;

        var expandedTask = ExpandBtn.RotateToSafe(0, 450);

        await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, MinHeight, 250);
        await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, MinWidth, 200);

        await expandedTask;

        ExpandBtn.Text = MaterialSymbolsRoundedIcons.Notes.GetGlyph();
        _navBarExpanded = false;
    }

    async void OnOverlayTapped(object? sender, TappedEventArgs e) => await CloseNavBarInternal();
    async void OnOverlayPan(object sender, PanUpdatedEventArgs e) => await CloseNavBarInternal();
    async void OnOverlayPinch(object sender, PinchGestureUpdatedEventArgs e) => await CloseNavBarInternal();
    void IgnoreOnOverlayTapped(object sender, TappedEventArgs e) { }

    async void OnNoticesClicked(object sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(LegalNoticesPage)));
    async void OnPrivacyClicked(object sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage)));
    async void OnTermsClicked(object sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(TermsOfUsePage)));
    async void OnAboutClicked(object sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(AboutPage)));

    async void OnDonateClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(DonatePage)));
    async void OnRefreshClicked(object? sender, EventArgs e) => await ButtonTap(() => RefreshClicked?.Invoke(this, EventArgs.Empty));
    async void OnHomeClicked(object? sender, EventArgs e) => await ButtonTap(() => HomeClicked?.Invoke(this, EventArgs.Empty));
    async void OnSettingsClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(SettingsPage)));

    async Task ButtonTap(Action action)
    {
        await CloseNavBarInternal();

        await Task.Delay(50);

        action.Invoke();
    }
}