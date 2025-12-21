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

        public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
        {
            if (navigationResponse.Response is NSHttpUrlResponse httpResponse)
            {
                int statusCode = (int)httpResponse.StatusCode;
                if (statusCode >= 400)
                {
                    _customWebView.RaiseError($"HTTP {statusCode}", GetHttpErrorMessage(statusCode));
                }
            }
            decisionHandler(WKNavigationResponsePolicy.Allow);
        }

        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        private static bool IsInternal(string? host)
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

        private static string GetHttpErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                // --- 1xx Informational ---
                100 => "Continuer",
                101 => "Changement de protocole",
                102 => "Traitement en cours",

                // --- 2xx Succès ---
                200 => "OK",
                201 => "Créé",
                202 => "Accepté",
                203 => "Informations non autorisées",
                204 => "Aucun contenu",
                205 => "Réinitialiser le contenu",
                206 => "Contenu partiel",
                207 => "Multi-Statut",
                208 => "Déjà signalé",
                226 => "IM utilisé",

                // --- 3xx Redirection ---
                300 => "Choix multiple",
                301 => "Déplacé définitivement",
                302 => "Trouvé",
                303 => "Voir autre",
                304 => "Non modifié",
                305 => "Utiliser un proxy",
                307 => "Redirection temporaire",
                308 => "Redirection permanente",

                // --- 4xx Erreurs client ---
                400 => "Requête incorrecte",
                401 => "Non autorisé",
                402 => "Paiement requis",
                403 => "Interdit",
                404 => "Introuvable",
                405 => "Méthode non autorisée",
                406 => "Non acceptable",
                407 => "Authentification proxy requise",
                408 => "Temps de requête dépassé",
                409 => "Conflit",
                410 => "Supprimé",
                411 => "Longueur requise",
                412 => "Précondition échouée",
                413 => "Charge utile trop volumineuse",
                414 => "URI trop longue",
                415 => "Type de média non supporté",
                416 => "Plage non satisfaisante",
                417 => "Échec de l'attente",
                418 => "Je suis une théière",
                421 => "Requête mal dirigée",
                422 => "Entité non traitable",
                423 => "Verrouillé",
                424 => "Dépendance échouée",
                425 => "Trop tôt",
                426 => "Mise à jour requise",
                428 => "Précondition requise",
                429 => "Trop de requêtes",
                431 => "Champs d'en-tête de requête trop grands",
                451 => "Indisponible pour des raisons légales",

                // --- 5xx Erreurs serveur ---
                500 => "Erreur interne du serveur",
                501 => "Non implémenté",
                502 => "Passerelle incorrecte",
                503 => "Service indisponible",
                504 => "Délai de la passerelle dépassé",
                505 => "Version HTTP non supportée",
                506 => "Variation aussi négociée",
                507 => "Stockage insuffisant",
                508 => "Boucle détectée",
                510 => "Non étendu",
                511 => "Authentification réseau requise",

                // --- Par défaut ---
                _ => $"Erreur HTTP inconnue ({statusCode})"
            };
        }
    }
}
