using Android.Content;
using Android.OS;
using Android.Util;
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using System.Runtime.Versioning;
using AndroidApp = Android.App;
using AndroidNet = Android.Net;
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
            handler.PlatformView.Settings.SetSupportMultipleWindows(true);
            handler.PlatformView.Settings.JavaScriptCanOpenWindowsAutomatically = true;

            handler.PlatformView?.SetWebChromeClient(
                    new ProgressClient(view)
                );
        });

        // Injecter le WebViewClient pour les erreurs HTTP
        CustomMapper.AppendToMapping("WebViewClient", (handler, view) =>
        {
            WebViewClient? defaultClient = null;
            if (OperatingSystem.IsAndroidVersionAtLeast(26)) // Android 8.0 (API 26)
            {
                defaultClient = handler.PlatformView?.WebViewClient;
            }

            if (view is CustomWebView customWebView)
            {
                handler.PlatformView?.SetWebViewClient(new CustomWebViewClient(defaultClient, view));
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
                MainThread.BeginInvokeOnMainThread(() => custom.Progress = newProgress / 100.0);
            }
        }

        public override bool OnCreateWindow(Webkit.WebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        {
            // On récupère ce que l'utilisateur a touché
            var hitTestResult = view?.GetHitTestResult();
            string? url = hitTestResult?.Extra;

            // Si c'est un lien valide, on l'ouvre dans le navigateur externe
            if (!string.IsNullOrEmpty(url))
            {
                if (IsImageUrl(url))
                {
                    return false;
                }

                if (IsInternalDomain(url))
                {
                    view?.LoadUrl(url); // On force le chargement interne
                    return false;
                }

                OpenInExternalBrowser(url!);
                return false; // On retourne false pour dire "n'ouvre pas de fenêtre interne dans l'app"
            }

            return base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
        }
    }

    private class CustomWebViewClient(WebViewClient? baseClient, CustomWebView customWebView) : WebViewClient
    {
        private readonly WebViewClient? _baseClient = baseClient;
        private readonly CustomWebView _customWebView = customWebView;

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
            if (_baseClient is null)
                base.OnReceivedError(view, request, error);
            else
                _baseClient?.OnReceivedError(view, request, error);

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
            if (_baseClient is null)
                base.OnReceivedHttpError(view, request, errorResponse);
            else
                _baseClient.OnReceivedHttpError(view, request, errorResponse);

            if (request?.IsForMainFrame != true || errorResponse == null)
                return;

            Log.Error("MyWebView", $"HTTP Error {errorResponse.StatusCode} {errorResponse.ReasonPhrase}");

            string message = GetHttpErrorMessage(errorResponse.StatusCode);
            _customWebView.RaiseError($"HTTP {errorResponse.StatusCode}", message);
        }

        public override bool ShouldOverrideUrlLoading(Webkit.WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return false;

            if (IsImageUrl(url))
            {
                return true;
            }

            // Analyse de l'URL
            if (IsInternalDomain(url))
            {
                return false;
            }

            // Tout le reste (pubs, liens externes, etc.) -> Navigateur Externe
            OpenInExternalBrowser(url);

            // On retourne true pour dire "J'ai géré le clic moi-même (en externe), ne charge rien dans la WebView"
            return true;
        }
    }

    private static bool IsInternalDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            var uri = new Uri(url);
            // On vérifie le domaine exact ou les sous-domaines
            return uri.Host.Equals("scan-manga.com", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.EndsWith(".scan-manga.com", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static bool IsImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // 1. Vérification par extension de fichier
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };
        if (extensions.Any(ext => url.Contains(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 2. Vérification par mots-clés (souvent utilisés par les serveurs de pubs ou CDN d'images)
        var keywords = new[] { "/uploads/", "/images/", "static.scan-manga", "img-manga" };
        if (keywords.Any(key => url.Contains(key, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private static void OpenInExternalBrowser(string url)
    {
        try
        {
            var intent = new Intent(Intent.ActionView, AndroidNet.Uri.Parse(url));
            intent.AddFlags(ActivityFlags.NewTask);
            AndroidApp.Application.Context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Error("CustomWebView", $"Impossible d'ouvrir le lien externe : {ex.Message}");
        }
    }

    private static string GetErrorMessage(int errorCode)
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
        _ => $"Erreur inconnue ({errorCode})"
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
