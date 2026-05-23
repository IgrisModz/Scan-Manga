#if ANDROID
using Android.Webkit;
#endif

namespace Scan_Manga.Helpers;

public static class WebErrorHelper
{
#if ANDROID
    public static WebErrorInfo GetErrorInfo(ClientError errorCode)
    {
        // Correction CA1416 : ClientError.UnsafeResource n'est disponible qu'à partir d'Android 26
        if (errorCode == ClientError.UnsafeResource)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                return new(
                    "Ressource non sécurisée (-16)",
                    "Le contenu demandé a été bloqué car il n'est pas sécurisé."
                );
            }
            // Pour Android < 26, on retourne une erreur générique
            return new(
                $"Erreur inconnue ({errorCode})",
                "Une erreur inconnue est survenue lors du chargement de la page."
            );
        }

        return errorCode switch
        {
            ClientError.TooManyRequests => new(
                "Trop de requêtes (-15)",
                "Vous avez effectué trop de requêtes en peu de temps. Veuillez réessayer plus tard."
            ),
            ClientError.FileNotFound => new(
                "Page introuvable (-14)",
                "Désolé, la page que vous cherchez n'existe pas ou a été déplacée."
            ),
            ClientError.File => new(
                "Erreur de fichier (-13)",
                "Une erreur est survenue lors de l'accès au fichier demandé."
            ),
            ClientError.BadUrl => new(
                "Adresse invalide (-12)",
                "L'adresse de la page est incorrecte ou mal formée."
            ),
            ClientError.FailedSslHandshake => new(
                "Connexion non sécurisée (-11)",
                "Le certificat de sécurité du site n'est pas valide."
            ),
            ClientError.UnsupportedScheme => new(
                "Schéma non supporté (-10)",
                "Le type de lien utilisé n'est pas pris en charge par l'application."
            ),
            ClientError.RedirectLoop => new(
                "Boucle de redirection (-9)",
                "La page redirige indéfiniment. Impossible de l'afficher."
            ),
            ClientError.Timeout => new(
                "Adresse invalide (-8)",
                "L'adresse de la page est incorrecte ou mal formée."
            ),
            ClientError.Io => new(
                "Erreur réseau (-7)",
                "Une erreur d'entrée/sortie est survenue lors de la communication réseau."
            ),
            ClientError.Connect => new(
                "Connexion impossible (-6)",
                "Impossible de se connecter au serveur."
            ),
            ClientError.ProxyAuthentication => new(
                "Authentification proxy requise (-5)",
                "Un proxy nécessite une authentification pour accéder à Internet."
            ),
            ClientError.Authentication => new(
                "Authentification requise (-4)",
                "Une authentification est nécessaire pour accéder à cette ressource."
            ),
            ClientError.UnsupportedAuthScheme => new(
                "Méthode d'authentification non supportée (-3)",
                "Le schéma d'authentification utilisé n'est pas pris en charge."
            ),
            ClientError.HostLookup => new(
                "Serveur introuvable (-2)",
                "Impossible de joindre le serveur. Vérifiez votre connexion ou l'adresse du site."
            ),
            ClientError.Unknown => new(
                "Erreur inconnue (-1)",
                "Une erreur inconnue est survenue lors du chargement de la page."
            ),
            // --- Fallback ---
            _ => new(
                $"Erreur de chargement ({errorCode})",
                "Une erreur inconnue est survenue lors du chargement de la page."
            )
        };
    }
#elif IOS
    static readonly Dictionary<int, WebErrorInfo> iosErrorMap = new()
    {
        [-1000] = new("URL invalide (-1000)", "L'adresse de la page est incorrecte."),
        [-1001] = new("Délai dépassé (-1001)", "Le serveur met trop de temps à répondre."),
        [-1003] = new("Serveur introuvable (-1003)", "Impossible de trouver le serveur demandé."),
        [-1004] = new("Impossible de se connecter (-1004)", "Le serveur est inaccessible pour le moment."),
        [-1005] = new("Connexion interrompue (-1005)", "La connexion réseau a été interrompue."),
        [-1007] = new("Redirection refusée (-1007)", "Trop de redirections ont été détectées."),
        [-1009] = new("Aucune connexion Internet (-1009)", "Vous n'êtes pas connecté à Internet. Vérifiez votre connexion réseau."),
        [-1012] = new("Authentification requise (-1012)", "Une authentification est nécessaire pour accéder à cette ressource."),
        [-1013] = new("Accès refusé (-1013)", "Vous n'avez pas les droits nécessaires pour accéder à cette ressource."),
        [-1100] = new("Page introuvable (-1100)", "La page demandée est introuvable ou a été déplacée."),
        [-1200] = new("Connexion non sécurisée (-1200)", "Le certificat de sécurité du site n'est pas valide."),
        [-1201] = new("Certificat expiré (-1201)", "Le certificat de sécurité du site a expiré."),
        [-1202] = new("Certificat invalide (-1202)", "Le certificat de sécurité du site est invalide."),
        [-1203] = new("Certificat non fiable (-1203)", "Le certificat de sécurité du site n'est pas approuvé."),
        [-1204] = new("Certificat requis (-1204)", "Un certificat client est requis pour accéder à cette ressource.")
    };

    public static WebErrorInfo GetErrorMessage(int errorCode)
    {
        return iosErrorMap.TryGetValue(errorCode, out var errorInfo)
            ? errorInfo
            : new($"Erreur de chargement ({errorCode})", "Une erreur inconnue est survenue lors du chargement de la page.");
    }
#endif

    static readonly Dictionary<int, WebErrorInfo> httpErrorMap = new()
    {
        [400] = new("Requête incorrecte", "La requête envoyée au serveur est invalide."),
        [401] = new("Non autorisé", "Vous devez être authentifié pour accéder à cette page."),
        [402] = new("Paiement requis", "Un paiement est nécessaire pour accéder à cette ressource."),
        [403] = new("Accès interdit", "Vous n'avez pas les droits nécessaires pour accéder à cette page."),
        [404] = new("Page introuvable", "Désolé, la page que vous cherchez n'existe pas ou a été déplacée."),
        [405] = new("Méthode non autorisée", "La méthode HTTP utilisée n'est pas autorisée pour cette ressource."),
        [406] = new("Non acceptable", "La ressource demandée ne peut pas être fournie dans ce format."),
        [407] = new("Authentification proxy requise", "Une authentification proxy est nécessaire."),
        [408] = new("Délai dépassé", "Le serveur a mis trop de temps à répondre."),
        [409] = new("Conflit", "La requête entre en conflit avec l'état actuel de la ressource."),
        [410] = new("Ressource supprimée", "La ressource demandée a été définitivement supprimée."),
        [411] = new("Longueur requise", "La requête doit spécifier la longueur du contenu."),
        [412] = new("Précondition échouée", "Les conditions préalables ne sont pas remplies."),
        [413] = new("Contenu trop volumineux", "La charge utile dépasse la taille autorisée."),
        [414] = new("URI trop longue", "L'adresse demandée est trop longue."),
        [415] = new("Type non supporté", "Le type de média de la requête n'est pas pris en charge."),
        [416] = new("Plage non satisfaisante", "La plage demandée n'est pas disponible."),
        [417] = new("Attente échouée", "Le serveur n'a pas pu satisfaire l'attente demandée."),
        [418] = new("Erreur inhabituelle", "Le serveur refuse la requête."),
        [421] = new("Requête mal dirigée", "La requête a été envoyée au mauvais serveur."),
        [422] = new("Entité non traitable", "Le serveur comprend la requête mais ne peut pas la traiter."),
        [423] = new("Ressource verrouillée", "La ressource est actuellement verrouillée."),
        [424] = new("Dépendance échouée", "Une dépendance requise a échoué."),
        [425] = new("Requête trop précoce", "La requête a été envoyée trop tôt."),
        [426] = new("Mise à jour requise", "Une mise à jour du protocole est requise."),
        [428] = new("Précondition requise", "Des conditions préalables sont requises."),
        [429] = new("Trop de requêtes", "Vous avez envoyé trop de requêtes en peu de temps."),
        [431] = new("En-têtes trop volumineux", "Les champs d'en-tête sont trop volumineux."),
        [451] = new("Indisponible légalement", "Cette ressource est indisponible pour des raisons légales."),
        [500] = new("Erreur serveur", "Le serveur a rencontré une erreur interne."),
        [501] = new("Non implémenté", "Cette fonctionnalité n'est pas prise en charge par le serveur."),
        [502] = new("Passerelle incorrecte", "Le serveur a reçu une réponse invalide."),
        [503] = new("Service indisponible", "Le service est temporairement indisponible."),
        [504] = new("Délai serveur dépassé", "Le serveur n'a pas répondu à temps."),
        [505] = new("Version HTTP non supportée", "La version HTTP utilisée n'est pas prise en charge."),
        [506] = new("Variation non négociée", "Une erreur de négociation de contenu est survenue."),
        [507] = new("Stockage insuffisant", "Le serveur ne dispose pas de suffisamment d'espace."),
        [508] = new("Boucle détectée", "Le serveur a détecté une boucle infinie."),
        [510] = new("Extension requise", "Une extension HTTP est requise."),
        [511] = new("Authentification réseau requise", "Une authentification réseau est nécessaire.")
    };

    public static WebErrorInfo GetHttpErrorInfo(int statusCode)
    {
        return httpErrorMap.TryGetValue(statusCode, out var errorInfo)
            ? errorInfo
            : new($"Erreur HTTP {statusCode}", "Une erreur HTTP inconnue est survenue.");
    }
}
