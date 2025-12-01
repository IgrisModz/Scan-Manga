using Foundation;
using Scan_Manga.Services;
using System.Diagnostics;
using System.Runtime.Versioning;
using UIKit;

namespace Scan_Manga.Platforms.iOS;

public class iOSFullscreenService : UIViewController, IFullScreenService
{
    // On garde une référence à notre contrôleur "fantôme"
    private UIViewController? _overlayViewController;

    // Propriété pour savoir si on est en plein écran
    public bool IsFullScreen => _overlayViewController != null;

    [SupportedOSPlatform("ios15.0")]
    [UnsupportedOSPlatform("maccatalyst")]
    public void EnterFullScreen()
    {
        // Si déjà actif, on sort
        if (IsFullScreen) return;

        // On doit toujours manipuler l'UI sur le MainThread
        NSOperationQueue.MainQueue.AddOperation(() =>
        {
            try
            {
                var parentController = GetVisibleViewController();
                if (parentController == null) return;

                // 1. On instancie notre contrôleur interne qui force les règles
                _overlayViewController = new()
                {
                    // 2. MAGIE ICI : On le rend totalement transparent
                    // OverCurrentContext permet de voir la page MAUI en dessous
                    ModalPresentationStyle = UIModalPresentationStyle.OverCurrentContext,
                    ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve
                };
                _overlayViewController.View!.BackgroundColor = UIColor.Clear;
                _overlayViewController.View.Opaque = false;

                // 3. On le présente par-dessus. iOS va lire ses propriétés (StatusBarHidden = true)
                parentController.PresentViewController(_overlayViewController, false, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[iOSFullscreenService] Erreur Enter: {ex.Message}");
                _overlayViewController = null;
            }
        });
    }

    [SupportedOSPlatform("ios15.0")]
    [UnsupportedOSPlatform("maccatalyst")]
    public void ExitFullScreen()
    {
        if (!IsFullScreen || _overlayViewController == null) return;

        NSOperationQueue.MainQueue.AddOperation(() =>
        {
            try
            {
                // On retire le masque transparent -> iOS réaffiche la barre d'état par défaut
                _overlayViewController.DismissViewController(false, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[iOSFullscreenService] Erreur Exit: {ex.Message}");
            }
            finally
            {
                // On nettoie la référence
                _overlayViewController = null;
            }
        });
    }

    [SupportedOSPlatform("ios15.0")]
    [UnsupportedOSPlatform("maccatalyst")]
    public void SetFullScreen(bool enable)
    {
        if (enable)
            EnterFullScreen();
        else
            ExitFullScreen();
    }

    private static UIViewController? GetVisibleViewController()
    {
        // 1. Récupération moderne de la fenêtre (iOS 13/15+)
        // On cherche dans les scènes connectées (UIWindowScene)
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        window ??= UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive)?
                .Windows.FirstOrDefault();

        var viewController = window?.RootViewController;

        while (viewController?.PresentedViewController != null)

        {// Éviter de se sélectionner soi-même si on appelle 2 fois la méthode
            if (viewController.PresentedViewController is HiddenStatusBarViewController)
                break;

            viewController = viewController.PresentedViewController;
        }

        return viewController;
    }

    public class HiddenStatusBarViewController : UIViewController
    {
        public override bool PrefersStatusBarHidden() => true;

        public override bool PrefersHomeIndicatorAutoHidden => true;

        public override UIStatusBarAnimation PreferredStatusBarUpdateAnimation
            => UIStatusBarAnimation.Fade;
    }

}
