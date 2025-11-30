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

    [SupportedOSPlatform("android23.0")]
    public void EnterFullScreen()
    {
        var activity = GetActivity();
        // Sécurisation null check
        if (activity?.Window == null) return;

        var window = activity.Window;

        activity.RunOnUiThread(() =>
        {
            try
            {
                // 1. Gestion du Notch (Encoche) - API 28+
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var attrs = window.Attributes;
                    if (attrs != null)
                    {
                        attrs.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                        window.Attributes = attrs;
                    }
                }

                // 2. Dire au système de laisser le contenu passer derrière les barres
                WindowCompat.SetDecorFitsSystemWindows(window, false);

                // 3. Application du masquage
                var decor = window.DecorView;
                if (decor == null) return;

                void ScheduleHide(int delayMs)
                {
                    if (delayMs <= 0)
                        decor.Post(() => ApplyHide(window));
                    else
                        decor.PostDelayed(() => ApplyHide(window), delayMs);
                }

                // Appels immédiats et différés (Anti-MIUI/OEM agressifs)
                ApplyHide(window);
                ScheduleHide(150);
                ScheduleHide(500);
                ScheduleHide(1000);
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
        var activity = GetActivity();
        if (activity?.Window == null) return;

        var window = activity.Window;

        activity.RunOnUiThread(() =>
        {
            try
            {
                // 1. Restaurer le comportement du Notch
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var attrs = window.Attributes;
                    if (attrs != null)
                    {
                        attrs.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;
                        window.Attributes = attrs;
                    }
                }

                // 2. Rendre le contrôle au système
                WindowCompat.SetDecorFitsSystemWindows(window, true);

                var decor = window.DecorView;
                if (decor == null) return;

                // 3. Afficher les barres via WindowCompat (API Moderne)
                var controller = WindowCompat.GetInsetsController(window, decor);
                if (controller != null)
                {
                    controller.Show(WindowInsetsCompat.Type.SystemBars());
                    controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
                }
                else
                {
                    // Fallback Legacy (avant API 30 ou si controller null)
                    // Suppression de l'avertissement car nécessaire pour les vieux Android
#pragma warning disable CS0618
                    decor.SystemUiVisibility = StatusBarVisibility.Visible;
#pragma warning restore CS0618
                }

                // 4. Nettoyage des flags WindowManager
                window.ClearFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);

                // 5. Restauration des couleurs
                // Ces méthodes sont obsolètes sur Android 35+ (Edge-to-Edge forcé), 
                // mais on les garde pour Android 23-34.
                if (!OperatingSystem.IsAndroidVersionAtLeast(35))
                {
                    try
                    {
                        window.SetStatusBarColor(Graphics.Color.Black);
                        window.SetNavigationBarColor(Graphics.Color.Black);
                    }
                    catch { /* Ignorer si non supporté */ }
                }
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
        var activity = GetActivity();
        if (activity?.Window == null) return;

        var window = activity.Window;

        activity.RunOnUiThread(() =>
        {
            try
            {
                bool isFull = false;

                // A. Vérification via Window Attributes
                var attrs = window.Attributes;
                if (attrs != null)
                {
                    isFull = (attrs.Flags & WindowManagerFlags.Fullscreen) != 0;
                }

                // B. Vérification fallback via SystemUiFlags (Legacy)
                if (!isFull)
                {
                    var decor = window.DecorView;
                    if (decor != null)
                    {
                        // On vérifie si on est sur une version < 30 AVANT d'accéder à la propriété
                        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
                        {
                            // Ce code ne s'exécutera que sur Android < 30, donc on peut ignorer l'alerte
                            var flags = decor.SystemUiFlags;
                            isFull = (flags & SystemUiFlags.Fullscreen) != 0;
                        }
                    }
                }

                if (isFull == enable)
                    return;

                if (enable)
                    EnterFullScreen();
                else
                    ExitFullScreen();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetFullScreen error: {ex.Message}");
            }
        });
    }

    private static void ApplyHide(Views.Window window)
    {
        try
        {
            var decor = window.DecorView;
            if (decor == null) return;

            if (!decor.IsAttachedToWindow)
            {
                decor.Post(() => ApplyHide(window));
                return;
            }

            // --- Modern Approach (API 30+) ---
            // On utilise WindowCompat qui gère la complexité
            var controller = WindowCompat.GetInsetsController(window, decor);
            if (controller != null)
            {
                controller.Hide(WindowInsetsCompat.Type.SystemBars());
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }

            // --- Paramètres Globaux Window ---
            WindowCompat.SetDecorFitsSystemWindows(window, false);
            window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.LayoutNoLimits);
            window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

            // Gestion Couleurs Transparente (Obsolète sur API 35, mais requis avant)
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                try
                {
                    window.SetStatusBarColor(Graphics.Color.Transparent);
                    window.SetNavigationBarColor(Graphics.Color.Transparent);
                }
                catch { }
            }

            // --- Legacy Approach (API < 30) ---
            // Le controller AndroidX fait souvent le travail, mais pour assurer le coup sur les vieux appareils
            // ou les surcouches agressives, on applique les flags directement.
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                // L'erreur mentionnait "Use SystemUiFlags property". 
                // C'est ici qu'on applique le changement.
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