using Foundation;
using WebKit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using UIKit;

namespace Scan_Manga.Platforms.iOS;

public class CustomWebViewHandler : WebViewHandler
{
    IDisposable? _progressObserver;

    public static IPropertyMapper<CustomWebView, CustomWebViewHandler> CustomMapper { get; } =
        new PropertyMapper<CustomWebView, CustomWebViewHandler>(Mapper);

    public static CommandMapper<CustomWebView, CustomWebViewHandler> CustomCommands { get; } =
        new CommandMapper<CustomWebView, CustomWebViewHandler>(CommandMapper);

    public CustomWebViewHandler() : base(CustomMapper, CustomCommands)
    {
        CustomMapper.AppendToMapping("iOSProgress", (handler, view) =>
        {
            if (handler.PlatformView != null && view is CustomWebView custom)
            {
                // callback correct avec 2 arguments
                _progressObserver = handler.PlatformView.AddObserver(
                    "estimatedProgress",
                    NSKeyValueObservingOptions.New,
                    change =>
                    {
                        if (view is CustomWebView custom)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (handler.PlatformView != null)
                                    custom.Progress = handler.PlatformView.EstimatedProgress;
                            });
                        }
                    });
            }
        });
    }

    protected override void ConnectHandler(WKWebView platformView)
    {
        base.ConnectHandler(platformView);
        if (VirtualView is CustomWebView customWebView)
        platformView.NavigationDelegate = new ExternalOnlyNavigationDelegate(customWebView);
    }

    protected override void DisconnectHandler(WKWebView platformView)
    {
        _progressObserver?.Dispose();
        _progressObserver = null;
        base.DisconnectHandler(platformView);
    }

    private class ExternalOnlyNavigationDelegate(CustomWebView customWebView) : WKNavigationDelegate, IWKUIDelegate
    {
        private readonly CustomWebView _customWebView = customWebView;

        [Export("webView:createWebViewWithConfiguration:forNavigationAction:windowFeatures:")]
        public WKWebView? CreateWebView(WKWebView webView, WKWebViewConfiguration configuration, WKNavigationAction navigationAction, WKWindowFeatures windowFeatures)
        {
            var nsUrl = navigationAction.Request.Url;

            if (nsUrl != null)
            {
                if (IsInternal(nsUrl.Host))
                {
                    // Si c'est interne (ex: un target=_blank sur le même site), on charge dans la vue actuelle
                    webView.LoadRequest(navigationAction.Request);
                }
                else
                {
                    // Si c'est externe, on ouvre Safari
                    UIApplication.SharedApplication.OpenUrl(nsUrl, new UIApplicationOpenUrlOptions(), null);
                }
            }

            return null; // Empêche l'ouverture d'une nouvelle fenêtre interne
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            var nsUrl = navigationAction.Request.Url;
            if (nsUrl == null)
            {
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            var host = nsUrl.Host?.ToLowerInvariant();

            // 1. Autoriser si c'est le domaine interne
            if (host == "scan-manga.com" || host?.EndsWith(".scan-manga.com") == true)
            {
                decisionHandler(WKNavigationActionPolicy.Allow);
                return;
            }

            // 2. Si c'est un lien externe ET que c'est un clic utilisateur (NavigationType == LinkActivated)
            // Cela évite que des redirections automatiques de pubs n'ouvrent Safari sans arrêt.
            if (navigationAction.NavigationType == WKNavigationType.LinkActivated)
            {
                UIApplication.SharedApplication.OpenUrl(nsUrl, new UIApplicationOpenUrlOptions(), null);
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            // Par défaut, on autorise (pour les ressources de la page, images, scripts)
            decisionHandler(WKNavigationActionPolicy.Allow);
        }

        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        private bool IsInternal(string? host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            host = host.ToLowerInvariant();
            return host == "scan-manga.com" || host.EndsWith(".scan-manga.com");
        }

        private void HandleError(NSError error)
        {
            // On ignore les erreurs d'annulation (quand on ouvre un lien externe par exemple)
            if (error.Domain == "NSURLErrorDomain" && error.Code == -999)
                return;

            string title = $"Erreur {error.Code}";
            string message = GetIoSErrorMessage((int)error.Code);

            _customWebView.RaiseError(title, message);
        }

        private static string GetIoSErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                -1001 => "Délai d'attente dépassé",
                -1003 => "Hôte introuvable",
                -1004 => "Impossible de se connecter au serveur",
                -1005 => "Connexion réseau perdue",
                -1009 => "Pas de connexion Internet",
                -1100 => "URL introuvable",
                -1200 => "Erreur SSL sécurisée",
                _ => $"Une erreur de communication est survenue ({errorCode})"
            };
        }
    }
}
