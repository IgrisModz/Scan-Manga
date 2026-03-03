using Android.App;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using Scan_Manga.Services;
using Views = Android.Views; // Evite les ambiguités

namespace Scan_Manga.Platforms.Android;

public class FullscreenService : IFullScreenService
{
    private int _defaultSystemUiVisibility;
    private bool _isSystemBarVisible;

    public bool IsFullScreen { get; set; } = false;

    public void EnterFullScreen()
    {
        // Si déjà en plein écran, on ne fait rien
        if (IsFullScreen) return;

        var activity = CurrentPlatformContext.CurrentActivity;
        var currentWindow = CurrentPlatformContext.CurrentWindow;

        activity.RunOnUiThread(() =>
        {
            try
            {
                // Gestion du Notch (API 28+)
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    currentWindow.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                }

                // Appels immédiats et différés (Anti-MIUI/OEM agressifs)
                var decorView = currentWindow.DecorView;
                if (decorView is null || !decorView.IsAttachedToWindow) return;
                
                ApplyHide(currentWindow, decorView);
                decorView.PostDelayed(() => ApplyHide(currentWindow, decorView), 150);
                decorView.PostDelayed(() => ApplyHide(currentWindow, decorView), 500);
                decorView.PostDelayed(() => ApplyHide(currentWindow, decorView), 1000);

                IsFullScreen = true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogPriority.Error, "AndroidFullscreenService", $"EnterFullScreen error: {ex.Message}");
            }
        });
    }

    public void ExitFullScreen()
    {
        if (!IsFullScreen) return;

        var activity = CurrentPlatformContext.CurrentActivity;
        var currentWindow = CurrentPlatformContext.CurrentWindow;

        activity.RunOnUiThread(() =>
        {
            try
            {
                var decorView = currentWindow.DecorView;
                if (decorView is null || !decorView.IsAttachedToWindow) return;
                var windowInsetsControllerCompat = WindowCompat.GetInsetsController(currentWindow, decorView);

                var barTypes = WindowInsetsCompat.Type.SystemBars()
                    | WindowInsetsCompat.Type.SystemBars()
                    | WindowInsetsCompat.Type.NavigationBars();

                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    if (_isSystemBarVisible)
                    {
                        currentWindow.InsetsController?.Show(WindowInsets.Type.SystemBars());
                    }
                }
                else
                {
                    decorView.SystemUiFlags = (SystemUiFlags)_defaultSystemUiVisibility;
                }

                if (windowInsetsControllerCompat is not null)
                {
                    windowInsetsControllerCompat.Show(barTypes);
                    windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
                }

                currentWindow.AddFlags(WindowManagerFlags.ForceNotFullscreen);
                currentWindow.ClearFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);

                // Restaurer le comportement du Notch
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    currentWindow.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;
                }

                WindowCompat.SetDecorFitsSystemWindows(currentWindow, true);

                IsFullScreen = false;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogPriority.Error, "AndroidFullscreenService", $"ExitFullScreen error: {ex.Message}");
            }
        });
    }

    public void SetFullScreen(bool enable)
    {
        if (enable)
            EnterFullScreen();
        else
            ExitFullScreen();
    }

    private void ApplyHide(Views.Window window, Views.View decorView)
    {
        try
        {
            var windowInsetsControllerCompat = WindowCompat.GetInsetsController(window, decorView);

            var barTypes = WindowInsetsCompat.Type.SystemBars()
                | WindowInsetsCompat.Type.SystemBars()
                | WindowInsetsCompat.Type.NavigationBars();

            WindowCompat.SetDecorFitsSystemWindows(window, false);
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var windowInsets = decorView.RootWindowInsets;
                if (windowInsets is null) return;

                _isSystemBarVisible = windowInsets.IsVisible(WindowInsetsCompat.Type.NavigationBars()) || windowInsets.IsVisible(WindowInsetsCompat.Type.StatusBars());

                if (_isSystemBarVisible)
                {
                    window.InsetsController?.Hide(WindowInsets.Type.SystemBars());
                }
            }
            else
            {
                _defaultSystemUiVisibility = (int)decorView.SystemUiFlags;

                window.DecorView.SystemUiFlags = decorView.SystemUiFlags
                    | SystemUiFlags.LayoutStable
                    | SystemUiFlags.LayoutHideNavigation
                    | SystemUiFlags.LayoutFullscreen
                    | SystemUiFlags.HideNavigation
                    | SystemUiFlags.Fullscreen
                    | SystemUiFlags.ImmersiveSticky;
            }

            window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);
            window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

            if (windowInsetsControllerCompat is not null)
            {
                windowInsetsControllerCompat.Hide(barTypes);
                windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine(LogPriority.Error, "AndroidFullscreenService", $"ApplyHide error: {ex.Message}");
        }
    }

    private readonly record struct CurrentPlatformContext()
    {
        public static Activity CurrentActivity => Platform.CurrentActivity ?? throw new InvalidOperationException("CurrentActivity cannot be null.");

        public static Views.Window CurrentWindow => CurrentActivity.Window ?? throw new InvalidOperationException("Window cannot be null.");
    }
}