using System.Runtime.Versioning;

using Android.Content;
using Android.OS;
using Android.Util;
using Android.Webkit;

using Microsoft.Maui.Handlers;

using Scan_Manga.Controls;
using Scan_Manga.Helpers;

using Scan_Manga.Controls;
using Scan_Manga.Helpers;

using AndroidApp = Android.App;
using AndroidNet = Android.Net;
using Webkit = Android.Webkit;

namespace Scan_Manga.Platforms.Android;

public partial class CustomWebViewHandler : WebViewHandler
{
    static readonly IPropertyMapper<CustomWebView, CustomWebViewHandler> customMapper =
        new PropertyMapper<CustomWebView, CustomWebViewHandler>(Mapper)
        {
        };

    static readonly CommandMapper<CustomWebView, CustomWebViewHandler> customCommands =
        new(CommandMapper)
        {
        };

    public CustomWebViewHandler() : base(customMapper, customCommands)
    {
        // =========================
        // CONFIGURATION CLOUDFLARE
        // =========================
        customMapper.AppendToMapping("Cloudflare", (handler, view) =>
        {
            var webView = handler.PlatformView;

            if (webView == null)
            {
                return;
            }

            var settings = webView.Settings;

            // JavaScript
            settings.JavaScriptEnabled = true;
            settings.JavaScriptCanOpenWindowsAutomatically = true;

            // Local Storage / Session Storage
            settings.DomStorageEnabled = true;
            settings.DatabaseEnabled = true;

            // Cookies
            CookieManager.Instance?.SetAcceptCookie(true);
            CookieManager.Instance?.SetAcceptThirdPartyCookies(webView, true);

            // Mixed content HTTP/HTTPS
            settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

            // Cache
            settings.CacheMode = CacheModes.Default;

            //// User-Agent type Chrome réel
            //settings.UserAgentString =
            //    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            //    "AppleWebKit/537.36 (KHTML, like Gecko) " +
            //    "Chrome/136.0.0.0 Safari/537.36";

            // Autorisations supplémentaires
            settings.AllowFileAccess = true;
            settings.AllowContentAccess = true;

            // Multiples fenêtres
            settings.SetSupportMultipleWindows(true);

            // Viewport
            settings.LoadWithOverviewMode = true;
            settings.UseWideViewPort = true;

            // Debug Chromium
#if DEBUG
            Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        });

        // =========================
        // WEB CHROME CLIENT
        // =========================
        customMapper.AppendToMapping("WebChromeClient", (handler, view) =>
        {
            handler.PlatformView.Settings.SetSupportMultipleWindows(true);
            handler.PlatformView.Settings.JavaScriptCanOpenWindowsAutomatically = true;

            handler.PlatformView?.SetWebChromeClient(
                new ProgressClient(view)
            );
        });

        // =========================
        // WEB VIEW CLIENT
        // =========================
        customMapper.AppendToMapping("WebViewClient", (handler, view) =>
        {
            WebViewClient? defaultClient = null;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                defaultClient = handler.PlatformView?.WebViewClient;
            }

            if (view is CustomWebView customWebView)
            {
                handler.PlatformView?.SetWebViewClient(
                    new CustomWebViewClient(defaultClient, view)
                );
            }
        });
    }

    class ProgressClient(CustomWebView webView) : WebChromeClient
    {
        readonly WeakReference<CustomWebView> refe = new(webView);

        public override void OnProgressChanged(Webkit.WebView? view, int newProgress)
        {
            base.OnProgressChanged(view, newProgress);

            if (refe.TryGetTarget(out var custom))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    custom.Progress = newProgress / 100.0);
            }
        }

        public override bool OnCreateWindow(
            Webkit.WebView? view,
            bool isDialog,
            bool isUserGesture,
            Message? resultMsg)
        {
            var hitTestResult = view?.GetHitTestResult();
            string? url = hitTestResult?.Extra;

            if (!string.IsNullOrEmpty(url))
            {
                if (IsImageUrl(url))
                {
                    return false;
                }

                if (IsInternalDomain(url))
                {
                    view?.LoadUrl(url);
                    return false;
                }

                OpenInExternalBrowser(url);
                return false;
            }

            return base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
        }
    }

    class CustomWebViewClient(
        WebViewClient? baseClient,
        CustomWebView customWebView) : WebViewClient
    {
        readonly WebViewClient? baseClient = baseClient;
        readonly CustomWebView customWebView = customWebView;

        public override void OnPageFinished(Webkit.WebView? view, string? url)
        {
            base.OnPageFinished(view, url);

            baseClient?.OnPageFinished(view, url);

            // Injection JS éventuelle après Cloudflare
            view?.EvaluateJavascript(@"
                window.open = function(url) {
                    window.location.href = url;
                };
            ", null);
        }

        [SupportedOSPlatform("android23.0")]
        public override void OnReceivedError(
            Webkit.WebView? view,
            IWebResourceRequest? request,
            WebResourceError? error)
        {
            if (baseClient is null)
            {
                base.OnReceivedError(view, request, error);
            }
            else
            {
                baseClient.OnReceivedError(view, request, error);
            }

            if (request?.IsForMainFrame != true || error == null)
            {
                return;
            }

            Log.Error("CustomWebView",
                $"OnReceivedError code={(int)error.ErrorCode} desc={error.Description}");

            var errorInfo = WebErrorHelper.GetErrorInfo(error.ErrorCode);

            customWebView.RaiseError(
                errorInfo.Title,
                errorInfo.Message);
        }

        [SupportedOSPlatform("android23.0")]
        public override void OnReceivedHttpError(
            Webkit.WebView? view,
            IWebResourceRequest? request,
            WebResourceResponse? errorResponse)
        {
            if (baseClient is null)
            {
                base.OnReceivedHttpError(view, request, errorResponse);
            }
            else
            {
                baseClient.OnReceivedHttpError(view, request, errorResponse);
            }

            if (request?.IsForMainFrame != true || errorResponse == null)
            {
                return;
            }

            Log.Error("CustomWebView",
                $"HTTP Error {errorResponse.StatusCode} {errorResponse.ReasonPhrase}");

            var errorInfo = WebErrorHelper.GetHttpErrorInfo(errorResponse.StatusCode);

            customWebView.RaiseError(
                $"{errorInfo.Title} ({errorResponse.StatusCode})",
                errorInfo.Message);
        }

        public override bool ShouldOverrideUrlLoading(
            Webkit.WebView? view,
            IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            // Cloudflare challenge
            if (url.Contains("/cdn-cgi/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IsImageUrl(url))
            {
                return true;
            }

            if (IsInternalDomain(url))
            {
                return false;
            }

            OpenInExternalBrowser(url);

            return true;
        }
    }

    static bool IsInternalDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            var uri = new Uri(url);

            return uri.Host.Equals(
                       "scan-manga.com",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   uri.Host.EndsWith(
                       ".scan-manga.com",
                       StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    static bool IsImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var extensions =
            new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };

        if (extensions.Any(ext =>
                url.Contains(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var keywords =
            new[] { "/uploads/", "/images/", "static.scan-manga", "img-manga" };

        return keywords.Any(key =>
            url.Contains(key, StringComparison.OrdinalIgnoreCase));
    }

    static void OpenInExternalBrowser(string url)
    {
        try
        {
            var intent = new Intent(
                Intent.ActionView,
                AndroidNet.Uri.Parse(url));

            intent.AddFlags(ActivityFlags.NewTask);

            AndroidApp.Application.Context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Error("CustomWebView",
                $"Impossible d'ouvrir le lien externe : {ex.Message}");
        }
    }
}
