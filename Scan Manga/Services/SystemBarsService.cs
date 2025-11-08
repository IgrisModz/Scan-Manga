using Android.Views;
using AndroidX.Core.View;
using System.Runtime.Versioning;

namespace Scan_Manga.Services;

public class SystemBarsService : ISystemBarsService
{
    public bool IsInitialized { get; private set; }
    public bool PreviousMode { get; private set; }

    [SupportedOSPlatform("android30.0")]
    public void SetLectureMode(bool enable)
    {
        if (IsInitialized && (!IsInitialized || PreviousMode == enable))
        {
            return;
        }

        var activity = Platform.CurrentActivity;
        var window = activity?.Window;

        if (window == null)
        {
            return;
        }

        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller != null)
        {

            if (enable)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                window.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                window.AddFlags(WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.Fullscreen);
                window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
            else
            {
                controller.Show(WindowInsets.Type.SystemBars());
                window.ClearFlags(WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.Fullscreen);
                window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
                window.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
            }

            PreviousMode = enable;
            IsInitialized = true;
        }
    }
}
