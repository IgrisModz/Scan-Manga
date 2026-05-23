using MauiIcons.Core.Extensions;
using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Helpers;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid
{
    public event EventHandler? RefreshClicked;
    public event EventHandler? HomeClicked;

    bool navBarExpanded = false;
    bool isVerticalExpanded = false;

    const double minWidth = 50;
    const double minHeight = 50;
    const double expandedHeight = 210;

    public ExpandableNavBar()
    {
        InitializeComponent();

        ExpandBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OnExpandClicked())
        });

        SafeAreaEdges = SafeAreaEdges.None;
    }

    async Task OnExpandClicked()
    {
        var screenWidth = Width > 0 ? Width :
                             DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        var maxWidth = Math.Min(screenWidth - 20, 400);

        if (!navBarExpanded)
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

            navBarExpanded = true;
        }
        else
        {
            await CloseNavBarInternal();
        }
    }

    async void OnMoreTapped(object? sender, TappedEventArgs e)
    {
        if (!isVerticalExpanded)
        {
            MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowDown.GetGlyph();

            await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, expandedHeight, 250);

            ExtraOptionsContainer.IsVisible = true;
            await ExtraOptionsContainer.FadeToSafe(1, 200);
            isVerticalExpanded = true;
        }
        else
        {
            await CloseVerticalMenu();
        }
    }

    async Task CloseVerticalMenu()
    {
		if (!isVerticalExpanded)
		{
			return;
		}

		MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph(); // Retour icône haut

        await ExtraOptionsContainer.FadeToSafe(0, 150);
        ExtraOptionsContainer.IsVisible = false;

        await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, 110, 200);

        isVerticalExpanded = false;
    }

    async Task CloseNavBarInternal()
    {
        ClickOutsideOverlay.IsVisible = false;

        // Si le menu vertical est ouvert, on le ferme d'abord
        if (isVerticalExpanded)
        {
            MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph(); // Retour icône haut

            await ExtraOptionsContainer.FadeToSafe(0, 150);
            ExtraOptionsContainer.IsVisible = false;

            isVerticalExpanded = false;
        }

        NavBarContent.IsVisible = false;
        MoreBtnContainer.IsVisible = false;
        ExpandBtnContainer.IsVisible = true;

        var expandedTask = ExpandBtn.RotateToSafe(0, 450);

        await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, minHeight, 250);
        await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, minWidth, 200);

        await expandedTask;

        ExpandBtn.Text = MaterialSymbolsRoundedIcons.Notes.GetGlyph();
        navBarExpanded = false;
    }

    async void OnOverlayTapped(object? sender, TappedEventArgs e) => await CloseNavBarInternal();
    async void OnOverlayPan(object? sender, PanUpdatedEventArgs e) => await CloseNavBarInternal();
    async void OnOverlayPinch(object? sender, PinchGestureUpdatedEventArgs e) => await CloseNavBarInternal();
    void IgnoreOnOverlayTapped(object? sender, TappedEventArgs e) { }

    async void OnNoticesClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(LegalNoticesPage)));
    async void OnPrivacyClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage)));
    async void OnTermsClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(TermsOfUsePage)));
    async void OnAboutClicked(object? sender, EventArgs e) => await ButtonTap(async () => await Shell.Current.GoToAsync(nameof(AboutPage)));

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