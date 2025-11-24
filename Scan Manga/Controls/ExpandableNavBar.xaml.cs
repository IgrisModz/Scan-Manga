using MauiIcons.Core;
using MauiIcons.Material;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid
{
    public event EventHandler? InfoTapped;
    public event EventHandler? RefreshTapped;

    private bool _navBarExpanded = false;

    public ExpandableNavBar()
    {
        InitializeComponent();
    }

    // --- Animations utilitaires ---
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

    // --- Actions utilisateur ---
    private async void OnExpandClicked(object? sender, EventArgs e)
    {
        double screenWidth = Width > 0 ? Width :
                             DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        double maxWidth = Math.Min(screenWidth - 20, 500);
        double minWidth = 50;

        if (!_navBarExpanded)
        {
            ClickOutsideOverlay.IsVisible = true;

            var rotation = ExpandBtn.RotateTo(360, 500);

            await AnimateWidth(NavBar, NavBar.Width, maxWidth, 200);
            await AnimateHeight(NavBar, NavBar.Height, 80, 300);

            await rotation;
            ExpandBtn.Icon(MaterialIcons.Close);

            NavBarContent.IsVisible = true;
            await NavBarContent.FadeTo(1, 200);

            _navBarExpanded = true;
        }
        else
        {
            ClickOutsideOverlay.IsVisible = false;

            await NavBarContent.FadeTo(0, 150);
            NavBarContent.IsVisible = false;

            var rotation = ExpandBtn.RotateTo(0, 450);

            await AnimateHeight(NavBar, NavBar.Height, 50, 250);
            await AnimateWidth(NavBar, NavBar.Width, minWidth, 200);

            await rotation;

            ExpandBtn.Icon(MaterialIcons.Notes);
            _navBarExpanded = false;
        }
    }

    private void OnOverlayTapped(object? sender, EventArgs e) => CloseNavBarIfOpen();
    private void OnOverlayPan(object sender, PanUpdatedEventArgs e) => CloseNavBarIfOpen();
    private void OnOverlayPinch(object sender, PinchGestureUpdatedEventArgs e) => CloseNavBarIfOpen();

    private async void OnInfoTapped(object sender, EventArgs e)
    {
        await InfoBtn.ScaleTo(0.85, 100, Easing.CubicInOut); // Rétrécit légèrement
        await InfoBtn.ScaleTo(1, 100, Easing.CubicInOut);

        // Reviens à la taille normale
        OnOverlayTapped(null, EventArgs.Empty);

        InfoTapped?.Invoke(this, EventArgs.Empty);
    }

    private async void OnRefreshTapped(object sender, EventArgs e)
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

        RefreshTapped?.Invoke(this, EventArgs.Empty);
    }

    private void CloseNavBarIfOpen()
    {
        if (_navBarExpanded)
        {
            OnExpandClicked(null, EventArgs.Empty);
        }
    }
}