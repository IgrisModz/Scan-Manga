using MauiIcons.Core;
using MauiIcons.Material;
using Scan_Manga.Helpers;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid
{
    public event EventHandler? RefreshTapped;
    public event EventHandler<TappedEventArgs>? HomeTapped;

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
            Command = new Command(async () => await OnExpandClicked(this, EventArgs.Empty))
        });

#if NET10_0_OR_GREATER
        SafeAreaEdges = SafeAreaEdges.None;
#else
        IgnoreSafeArea = true;
#endif
    }

    private async Task OnExpandClicked(object? sender, EventArgs e)
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

            ExpandBtn.Icon(MaterialIcons.Close);
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
            MoreIcon.Icon(MaterialIcons.KeyboardArrowDown);

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

        MoreIcon.Icon(MaterialIcons.KeyboardArrowUp); // Retour icône haut

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
            await CloseVerticalMenu();
            await Task.Yield();
        }

        NavBarContent.IsVisible = false;
        MoreBtnContainer.IsVisible = false;
        ExpandBtnContainer.IsVisible = true;

        var expandedTask = ExpandBtn.RotateToSafe(0, 450);

        await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, MinHeight, 250);
        await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, MinWidth, 200);

        await expandedTask;

        ExpandBtn.Icon(MaterialIcons.Notes);
        _navBarExpanded = false;
    }

    private async void OnOverlayTapped(object? sender, TappedEventArgs e) => await CloseNavBarInternal();
    private async void OnOverlayPan(object sender, PanUpdatedEventArgs e) => await CloseNavBarInternal();
    private async void OnOverlayPinch(object sender, PinchGestureUpdatedEventArgs e) => await CloseNavBarInternal();
    private void IgnoreOnOverlayTapped(object sender, TappedEventArgs e) { }

    private async void OnRefreshTapped(object sender, TappedEventArgs e) =>
        await HandleInteractionAsync(sender, () => { RefreshTapped?.Invoke(this, EventArgs.Empty); return Task.CompletedTask; }, true);
    public async void OnHomeTapped(object sender, TappedEventArgs e) =>
        await HandleInteractionAsync(sender, () => { HomeTapped?.Invoke(this, e); return Task.CompletedTask; });
    
    private async void OnDonateTapped(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(DonatePage));
    private async void OnSettingsTapped(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(SettingsPage), true);

    private async void OnNoticesClicked(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(LegalNoticesPage));
    private async void OnPrivacyClicked(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(PrivacyPolicyPage));
    private async void OnTermsClicked(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(TermsOfUsePage));
    private async void OnAboutClicked(object sender, TappedEventArgs e) =>
        await NavigateToAsync(sender, nameof(AboutPage));

    private Task NavigateToAsync(object sender, string route, bool rotate = false)
    {
        return HandleInteractionAsync(sender, async () =>
        {
            await Shell.Current.GoToAsync(route);
        }, rotate);
    }

    private async Task HandleInteractionAsync(object sender, Func<Task> action, bool rotateIcon = false)
    {
        if (sender is not VisualElement view) return;

        await view.ScaleToSafe(0.7, 100);
        await view.ScaleToSafe(1, 100);

        if (rotateIcon && view is VerticalStackLayout stack)
        {
            var icon = stack.Children.OfType<Label>().FirstOrDefault();
            if (icon != null) await icon.RotateToSafe(360, 500);
        }

        await CloseNavBarInternal();

        await Task.Delay(50);

        await action.Invoke();
    }
}