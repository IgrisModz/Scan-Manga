using Android.Util;
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using System.Runtime.Versioning;
using Webkit = Android.Webkit;

namespace Scan_Manga.Platforms.Android;

public class CustomWebViewHandler : WebViewHandler
{
    private readonly static IPropertyMapper<CustomWebView, CustomWebViewHandler> CustomMapper =
            new PropertyMapper<CustomWebView, CustomWebViewHandler>(Mapper)
            {
            };

    private readonly static CommandMapper<CustomWebView, CustomWebViewHandler> CustomCommands =
        new(CommandMapper)
        {
        };

    public CustomWebViewHandler() : base(CustomMapper, CustomCommands)
    {
        // Injecter le WebChromeClient APRÈS le mapping MAUI
        CustomMapper.AppendToMapping("WebChromeClient", (handler, view) =>
        {
            handler.PlatformView?.SetWebChromeClient(
                    new ProgressClient(view)
                );
        });

        // Injecter le WebViewClient pour les erreurs HTTP
        CustomMapper.AppendToMapping("WebViewClient", (handler, view) =>
        {

            if (view is CustomWebView customWebView)
            {
                var defaultClient = handler.PlatformView.WebViewClient;
                handler.PlatformView?.SetWebViewClient(new ErrorClient(defaultClient, view));
            }
        });
    }

    private class ProgressClient(CustomWebView webView) : WebChromeClient
    {
        readonly WeakReference<CustomWebView> _ref = new(webView);

        public override void OnProgressChanged(Webkit.WebView? view, int newProgress)
        {
            base.OnProgressChanged(view, newProgress);

            if (_ref.TryGetTarget(out var custom))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    custom.Progress = newProgress / 100.0;
                });
            }
        }
    }

    private class ErrorClient(WebViewClient baseClient, CustomWebView handler) : WebViewClient
    {
        private readonly WebViewClient? _baseClient = baseClient;
        private readonly CustomWebView _customWebView = handler;

        public override void OnPageFinished(Webkit.WebView? view, string? url)
        {
            _baseClient?.OnPageFinished(view, url);
        }

        [SupportedOSPlatform("android23.0")]
        public override void OnReceivedError(
                Webkit.WebView? view,
                IWebResourceRequest? request,
                WebResourceError? error)
        {
            if (_baseClient == null)
            {
                base.OnReceivedError(view, request, error);
            }
            else
            {
                _baseClient?.OnReceivedError(view, request, error);
            }

            if (request?.IsForMainFrame != true || error == null)
                return;

            Log.Error("CustomWebView", $"OnReceivedError code={(int)error.ErrorCode} desc={error.Description}");

            string message = GetErrorMessage((int)error.ErrorCode);
            _customWebView.RaiseError($"Erreur {error.ErrorCode}", message);
        }

        [SupportedOSPlatform("android23.0")]
        public override void OnReceivedHttpError(
            Webkit.WebView? view,
            IWebResourceRequest? request,
            WebResourceResponse? errorResponse)
        {
            if (_baseClient == null)
            {
                base.OnReceivedHttpError(view, request, errorResponse);
            }
            else
            {
                _baseClient?.OnReceivedHttpError(view, request, errorResponse);
            }

            if (request?.IsForMainFrame != true || errorResponse == null)
                return;

            Log.Error("MyWebView", $"HTTP Error {errorResponse.StatusCode} {errorResponse.ReasonPhrase}");

            string message = GetHttpErrorMessage(errorResponse.StatusCode);
            _customWebView.RaiseError($"HTTP {errorResponse.StatusCode}", message);
        }
    }

    public static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            -16 => "Ressource non sécurisée",
            -15 => "Trop de requêtes",
            -14 => "Fichier introuvable",
            -13 => "Erreur de fichier",
            -12 => "URL invalide",
            -11 => "Erreur SSL",
            -10 => "Schéma non supporté",
            -9 => "Boucle de redirection",
            -8 => "Délai dépassé",
            -7 => "Erreur d'entrée/sortie",
            -6 => "Impossible de se connecter",
            -5 => "Authentification proxy requise",
            -4 => "Erreur d'authentification",
            -3 => "Schéma d'authentification non supporté",
            -2 => "Hôte introuvable",
            -1 => "Erreur inconnue",
        _ => $"Erreur inconnue ({errorCode})" }; }

    public static string GetHttpErrorMessage(int statusCode)
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
