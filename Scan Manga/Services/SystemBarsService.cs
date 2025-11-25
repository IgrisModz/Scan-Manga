#if ANDROID
using Android.Views;
using AndroidX.Core.View;
using System.Runtime.Versioning;

namespace Scan_Manga.Services;

public class SystemBarsService : ISystemBarsService
{
    public bool IsInitialized { get; private set; }
    public bool PreviousMode { get; private set; }

    public SystemBarsMode CurrentMode =>
        IsInitialized ? (PreviousMode ? SystemBarsMode.Lecture : SystemBarsMode.Default) : SystemBarsMode.Default;

    [SupportedOSPlatform("android23.0")]
    public void SetLectureMode(bool enable)
    {
        if (IsInitialized && PreviousMode == enable)
        {
            return;
        }

        if (enable ? HideSystemBars() : ShowSystemBars())
        {
            PreviousMode = enable;
            IsInitialized = true;
        }
    }

    [SupportedOSPlatform("android23.0")]
    private static bool ShowSystemBars()
    {
        var activity = Platform.CurrentActivity;
        var window = activity?.Window;
        var decorView = window?.DecorView;

        if (activity is null || window is null || decorView is null) return false;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var controller = WindowCompat.GetInsetsController(window, window.DecorView);

            if (controller is null) return false;

            controller.Show(WindowInsets.Type.SystemBars());
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;

            window.ClearFlags(WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.Fullscreen);

        }
        else
        {
#pragma warning disable CS0618

            decorView.SystemUiVisibility = StatusBarVisibility.Visible;
#pragma warning restore CS0618
        }

        return true;
    }

    [SupportedOSPlatform("android23.0")]
    private static bool HideSystemBars()
    {
        var activity = Platform.CurrentActivity;
        var window = activity?.Window;
        var decorView = window?.DecorView;

        if (activity is null || window is null || decorView is null) return false;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var controller = WindowCompat.GetInsetsController(window, window.DecorView);

            if (controller is null) return false;

            controller?.Hide(WindowInsets.Type.SystemBars());
            controller?.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;

            window.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            window.AddFlags(WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.Fullscreen);
        }
        else
        {
#pragma warning disable CS0618
            decorView.SystemUiVisibility =
                    (StatusBarVisibility)(
                        SystemUiFlags.LayoutStable |
                        SystemUiFlags.LayoutFullscreen |
                        SystemUiFlags.Fullscreen |
                        SystemUiFlags.HideNavigation |
                        SystemUiFlags.ImmersiveSticky
                    );
#pragma warning restore CS0618
        }

        return true;
    }
}
#endif