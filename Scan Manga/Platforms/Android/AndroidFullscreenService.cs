using Android.Views;
using AndroidX.Core.View;
using Scan_Manga.Services;
using System.Runtime.Versioning;
using Graphics = Android.Graphics;
using Debug = System.Diagnostics.Debug;
using AndroidApp = Android.App;
using Views = Android.Views;

// Note: Assurez-vous que le namespace correspond bien à votre projet
[assembly: Dependency(typeof(Scan_Manga.Platforms.Android.AndroidFullscreenService))]
namespace Scan_Manga.Platforms.Android;

public class AndroidFullscreenService : IFullScreenService
{
    private static AndroidApp.Activity? GetActivity() => Platform.CurrentActivity;
    public bool IsFullScreen { get; set; } = false;


    [SupportedOSPlatform("android23.0")]
    public void EnterFullScreen()
    {
        // Si déjà en plein écran, on ne fait rien
        if (IsFullScreen) return;

        var activity = GetActivity();
        if (activity?.Window == null) return;

        var window = activity.Window;
        activity.RunOnUiThread(() =>
        {
            try
            {
                // Gestion du Notch (API 28+)
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var attrs = window.Attributes;
                    if (attrs != null)
                    {
                        attrs.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                        window.Attributes = attrs;
                    }
                }

                WindowCompat.SetDecorFitsSystemWindows(window, false);

                // Appels immédiats et différés (Anti-MIUI/OEM agressifs)
                var decor = window.DecorView;
                if (decor == null) return;

                ApplyHide(window);
                decor.PostDelayed(() => ApplyHide(window), 150);
                decor.PostDelayed(() => ApplyHide(window), 500);
                decor.PostDelayed(() => ApplyHide(window), 1000);

                IsFullScreen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnterFullScreen error: {ex.Message}");
            }
        });
    }

    [SupportedOSPlatform("android23.0")]
    public void ExitFullScreen()
    {
        // Si déjà sorti du plein écran, on ne fait rien
        if (!IsFullScreen) return;

        var activity = GetActivity();
        if (activity?.Window == null) return;

        var window = activity.Window;
        activity.RunOnUiThread(() =>
        {
            try
            {
                // Restaurer le comportement du Notch
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var attrs = window.Attributes;
                    if (attrs != null)
                    {
                        attrs.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;
                        window.Attributes = attrs;
                    }
                }

                WindowCompat.SetDecorFitsSystemWindows(window, true);

                var decor = window.DecorView;
                if (decor == null) return;

                // Afficher les barres
                var controller = WindowCompat.GetInsetsController(window, decor);
                if (controller != null)
                {
                    controller.Show(WindowInsetsCompat.Type.SystemBars());
                    controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
                }
                else
                {
#pragma warning disable CS0618
                    decor.SystemUiVisibility = StatusBarVisibility.Visible;
#pragma warning restore CS0618
                }

                window.ClearFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);

                // Restauration des couleurs (Android 23-34)
                if (!OperatingSystem.IsAndroidVersionAtLeast(35))
                {
                    window.SetStatusBarColor(Graphics.Color.Black);
                    window.SetNavigationBarColor(Graphics.Color.Black);
                }

                IsFullScreen = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExitFullScreen error: {ex.Message}");
            }
        });
    }

    [SupportedOSPlatform("android23.0")]
    public void SetFullScreen(bool enable)
    {
        if (enable)
            EnterFullScreen();
        else
            ExitFullScreen();
    }

    private static void ApplyHide(Views.Window window)
    {
        try
        {
            var decor = window.DecorView;
            if (decor == null || !decor.IsAttachedToWindow) return;

            // Modern Approach (API 30+)
            var controller = WindowCompat.GetInsetsController(window, decor);
            if (controller != null)
            {
                controller.Hide(WindowInsetsCompat.Type.SystemBars());
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }

            WindowCompat.SetDecorFitsSystemWindows(window, false);
            window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);
            window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

            // Couleurs transparentes (Android 23-34)
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                window.SetStatusBarColor(Graphics.Color.Transparent);
                window.SetNavigationBarColor(Graphics.Color.Transparent);
            }

            // Legacy Approach (API < 30)
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var uiOptions = SystemUiFlags.LayoutStable
                                | SystemUiFlags.LayoutHideNavigation
                                | SystemUiFlags.LayoutFullscreen
                                | SystemUiFlags.HideNavigation
                                | SystemUiFlags.Fullscreen
                                | SystemUiFlags.ImmersiveSticky;

                decor.SystemUiFlags = uiOptions;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyHide error: {ex.Message}");
        }
    }
}