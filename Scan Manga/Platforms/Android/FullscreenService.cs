using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Scan_Manga.Services;
using Color = Android.Graphics.Color;
using View = Android.Views.View;
using Window = Android.Views.Window;

namespace Scan_Manga.Platforms.Android;

public class FullscreenService : IFullScreenService
{
    private int _defaultSystemUiVisibility;
    private bool _wasSystemBarVisible;
    private int _originalStatusBarColor;
    private int _originalNavigationBarColor;

    // Listes pour gérer les overlays multiples (bug CommunityToolkit.Maui: StatusBar n'a pas de tag, donc plusieurs overlays peuvent être créés)
    private readonly List<View> _statusBarOverlays = [];
    private View? _navigationBarOverlay;

    public bool IsFullScreen { get; set; }

    public void SetFullScreen(bool enable)
    {
        if (enable) EnterFullScreen();
        else ExitFullScreen();
    }

    public void EnterFullScreen() 
    {
        if (IsFullScreen) return;

        RunOnUiThread(() =>
        {
            if (CurrentWindow is not { DecorView: { IsAttachedToWindow: true } decorView } window) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                window.Attributes!.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

            ApplyHide(window, decorView);

            // Appels différés pour contourner les OEM agressifs (MIUI, etc.)
            foreach (var delay in (int[])[150, 500, 1000])
                decorView.PostDelayed(() => ApplyHide(window, decorView), delay);

            IsFullScreen = true;
        });
    }

    public void ExitFullScreen()
    {
        if (!IsFullScreen) return;

        RunOnUiThread(() =>
        {
            if (CurrentWindow is not { DecorView: { IsAttachedToWindow: true } decorView } window) return;

            RestoreSystemBars(window, decorView);
            IsFullScreen = false;
        });
    }

    private void ApplyHide(Window window, View decorView)
    {
        var controller = WindowCompat.GetInsetsController(window, decorView);
        var barTypes = WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.NavigationBars();

        // Android 35+ : Masquer les overlays CommunityToolkit.Maui
        if (OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            var decorGroup = (ViewGroup)decorView;

            // Masquer TOUS les StatusBar overlays (bug CTK: plusieurs peuvent exister car pas de tag)
            foreach (var overlay in FindAllStatusBarOverlays(decorGroup))
            {
                overlay.Visibility = ViewStates.Gone;
                if (!_statusBarOverlays.Contains(overlay))
                    _statusBarOverlays.Add(overlay);
            }

            if ((_navigationBarOverlay = decorGroup.FindViewWithTag("NavigationBarOverlay") as View) is not null)
                _navigationBarOverlay.Visibility = ViewStates.Gone;

            SetBarColors(window, Color.Transparent, Color.Transparent);
            window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            (_originalStatusBarColor, _originalNavigationBarColor) = (window.StatusBarColor, window.NavigationBarColor);
            SetBarColors(window, Color.Transparent, Color.Transparent);
        }

        WindowCompat.SetDecorFitsSystemWindows(window, false);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (decorView.RootWindowInsets is not { } insets) return;
            _wasSystemBarVisible = insets.IsVisible(WindowInsetsCompat.Type.NavigationBars()) || insets.IsVisible(WindowInsetsCompat.Type.StatusBars());
            if (_wasSystemBarVisible) window.InsetsController?.Hide(WindowInsets.Type.SystemBars());
        }
        else
        {
            _defaultSystemUiVisibility = (int)decorView.SystemUiFlags;
            decorView.SystemUiFlags = SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation 
                | SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation 
                | SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky;
        }

        window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);
        window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
        controller?.Hide(barTypes);
        controller?.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }

    private void RestoreSystemBars(Window window, View decorView)
    {
        var controller = WindowCompat.GetInsetsController(window, decorView);
        var barTypes = WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.NavigationBars();

        if (OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
            window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);

            // Restaurer TOUS les StatusBar overlays
            foreach (var overlay in _statusBarOverlays)
                overlay.Visibility = ViewStates.Visible;
            _statusBarOverlays.Clear();

            _navigationBarOverlay?.Visibility = ViewStates.Visible;
            _navigationBarOverlay = null;
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            SetBarColors(window, new Color(_originalStatusBarColor), new Color(_originalNavigationBarColor));
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (_wasSystemBarVisible) window.InsetsController?.Show(WindowInsets.Type.SystemBars());
        }
        else
        {
            decorView.SystemUiFlags = (SystemUiFlags)_defaultSystemUiVisibility;
        }

        controller?.Show(barTypes);
        controller?.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;

        window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
        window.ClearFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);

        if (OperatingSystem.IsAndroidVersionAtLeast(28))
            window.Attributes!.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;

        WindowCompat.SetDecorFitsSystemWindows(window, true);
    }

    #region Helpers

    private static Window? CurrentWindow => Platform.CurrentActivity?.Window;

    private static void RunOnUiThread(Action action) => Platform.CurrentActivity?.RunOnUiThread(() =>
    {
        try { action(); }
        catch (Exception ex) { Log.Error("FullscreenService", ex.Message); }
    });

    private static void SetBarColors(Window window, Color statusBar, Color navigationBar)
    {
#pragma warning disable CA1416, CA1422
        window.SetStatusBarColor(statusBar);
        window.SetNavigationBarColor(navigationBar);
#pragma warning restore CA1416, CA1422
    }

    /// <summary>
    /// Trouve TOUS les StatusBarOverlays de CommunityToolkit.Maui.
    /// Bug CTK: StatusBar n'a pas de tag défini, donc FindViewWithTag ne fonctionne pas
    /// et un nouvel overlay est créé à chaque changement de couleur → plusieurs overlays empilés.
    /// </summary>
    private static List<View> FindAllStatusBarOverlays(ViewGroup decorGroup)
    {
        var overlays = new List<View>();
        var resources = Platform.CurrentActivity?.Resources;
        if (resources is null) return overlays;

        var heightId = resources.GetIdentifier("status_bar_height", "dimen", "android");
        var expectedHeight = (heightId > 0 ? resources.GetDimensionPixelSize(heightId) : 0) + 3;

        for (var i = 0; i < decorGroup.ChildCount; i++)
        {
            if (decorGroup.GetChildAt(i) is { LayoutParameters: FrameLayout.LayoutParams { Gravity: GravityFlags.Top, Width: ViewGroup.LayoutParams.MatchParent } lp } child
                && lp.Height == expectedHeight)
                overlays.Add(child);
        }
        return overlays;
    }

    #endregion
}