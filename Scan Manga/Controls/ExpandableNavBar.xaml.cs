using MauiIcons.Core;
using MauiIcons.Material;
using Scan_Manga.Helpers;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid
{
    public event EventHandler? InfoTapped;
    public event EventHandler? RefreshTapped;
    public event EventHandler? HomeTapped;

    private bool _navBarExpanded = false;

    public ExpandableNavBar()
    {
        InitializeComponent();

#if NET10_0_OR_GREATER
        SafeAreaEdges = SafeAreaEdges.None;
#else
        IgnoreSafeArea = true;
#endif
    }

    // --- Actions utilisateur ---
    private async void OnExpandClicked(object? sender, EventArgs e)
    {
        double screenWidth = Width > 0 ? Width :
                             DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        double maxWidth = Math.Min(screenWidth - 20, 500);
        double minWidth = 50;

        if (!_navBarExpanded)
        {
            // Ensure ScrollView is at start before opening
            try
            {
                await NavBarScroll.ScrollToAsync(0, 0, false);
            }
            catch { }

            ClickOutsideOverlay.IsVisible = true;

            var rotationTask = ExpandBtn.RotateToSafe(360, 500);

            await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, maxWidth, 200);
            await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, 80, 300);

            await rotationTask;
            ExpandBtn.Icon(MaterialIcons.Close);

            NavBarContent.IsVisible = true;

            // Fade in buttons sequentially
            await InfoBtn.FadeToSafe(1, 120);
            await DonateBtn.FadeToSafe(1, 120);
            await RefreshBtn.FadeToSafe(1, 120);
            await HomeBtn.FadeToSafe(1, 120);
            await SettingsBtn.FadeToSafe(1, 120);

            _navBarExpanded = true;
        }
        else
        {
            ClickOutsideOverlay.IsVisible = false;

            // Fade out buttons sequentially
            await SettingsBtn.FadeToSafe(1, 120);
            await HomeBtn.FadeToSafe(1, 120);
            await RefreshBtn.FadeToSafe(0, 120);
            await DonateBtn.FadeToSafe(0, 120);
            await InfoBtn.FadeToSafe(0, 120);

            NavBarContent.IsVisible = false;

            var rotationTask = ExpandBtn.RotateToSafe(0, 450);

            await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, 50, 250);
            await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, minWidth, 200);

            await rotationTask;

            ExpandBtn.Icon(MaterialIcons.Notes);

            _navBarExpanded = false;

            // Reset ScrollView to start after closing
            try
            {
                await NavBarScroll.ScrollToAsync(0, 0, false);
            }
            catch { }
        }
    }

    private void OnOverlayTapped(object? sender, EventArgs e) => CloseNavBarIfOpen();
    private void OnOverlayPan(object sender, PanUpdatedEventArgs e) => CloseNavBarIfOpen();
    private void OnOverlayPinch(object sender, PinchGestureUpdatedEventArgs e) => CloseNavBarIfOpen();

    private async void OnInfoTapped(object sender, EventArgs e) => await ButtonTap(sender, () => InfoTapped?.Invoke(this, EventArgs.Empty));
    private async void OnRefreshTapped(object sender, EventArgs e) => await ButtonTapWithLabelRotation(sender, () => RefreshTapped?.Invoke(this, EventArgs.Empty));
    private async void OnDonateTapped(object sender, TappedEventArgs e) => await ButtonTap(sender, async () => await Shell.Current.GoToAsync(nameof(DonatePage)));
    public async void OnHomeTapped(object sender, TappedEventArgs e) => await ButtonTap(sender, () => HomeTapped?.Invoke(this, EventArgs.Empty));
    private async void OnSettingsTapped(object sender, TappedEventArgs e) => await ButtonTapWithLabelRotation(sender, async () => await Shell.Current.GoToAsync(nameof(SettingsPage)));

    private async Task ButtonTap(object sender, Action action)
    {
        if (sender is VerticalStackLayout tappedBtn)
        {
            await tappedBtn.ScaleToSafe(0.70, 100);
            await tappedBtn.ScaleToSafe(1, 100);

            CloseNavBarIfOpen();

            action.Invoke();
        }
    }

    private async Task ButtonTapWithLabelRotation(object sender, Action action)
    {
        if (sender is VerticalStackLayout tappedBtn)
        {
            var btnLabel = tappedBtn.Children.OfType<Label>().First();
            btnLabel.Rotation = 0;
            var rotationTask = btnLabel.RotateToSafe(360, 500);

            await tappedBtn.ScaleToSafe(0.70, 100);
            await tappedBtn.ScaleToSafe(1, 100);

            // Ensure rotation completes before continuing
            await rotationTask;

            CloseNavBarIfOpen();

            action.Invoke();
        }

    }

    private void CloseNavBarIfOpen()
    {
        if (_navBarExpanded)
        {
            OnExpandClicked(null, EventArgs.Empty);
        }
    }
}