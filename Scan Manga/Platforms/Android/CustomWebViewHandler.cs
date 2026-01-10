using Android.Content;
using Android.OS;
using Android.Util;
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using Scan_Manga.Helpers;
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

            var errorInfo = WebErrorHelper.GetErrorInfo(error.ErrorCode);
            _customWebView.RaiseError(errorInfo.Title, errorInfo.Message);
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

            var errorInfo = WebErrorHelper.GetHttpErrorInfo(errorResponse.StatusCode);
            _customWebView.RaiseError($"{errorInfo.Title} ({errorResponse.StatusCode})", errorInfo.Message);
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

}
