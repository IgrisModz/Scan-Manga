using Foundation;
using WebKit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using UIKit;
using Scan_Manga.Helpers;

namespace Scan_Manga.Platforms.iOS;

public class CustomWebViewHandler : WebViewHandler
{
    IDisposable? progressObserver;

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
                progressObserver = handler.PlatformView.AddObserver(
                    "estimatedProgress",
                    NSKeyValueObservingOptions.New,
                    change =>
                    {
                        if (view is CustomWebView custom)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (handler.PlatformView != null)
								{
									custom.Progress = handler.PlatformView.EstimatedProgress;
								}
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
		{
			platformView.NavigationDelegate = new ExternalOnlyNavigationDelegate(customWebView);
		}
	}

    protected override void DisconnectHandler(WKWebView platformView)
    {
        progressObserver?.Dispose();
        progressObserver = null;
        base.DisconnectHandler(platformView);
    }

    class ExternalOnlyNavigationDelegate(CustomWebView customWebView) : WKNavigationDelegate, IWKUIDelegate
    {
        readonly CustomWebView customWebView = customWebView;

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
                    var errorInfo = WebErrorHelper.GetHttpErrorInfo(statusCode);
                    customWebView.RaiseError($"{errorInfo.Title} ({statusCode})", errorInfo.Message);
                }
            }
            decisionHandler(WKNavigationResponsePolicy.Allow);
        }

        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error) => HandleError(error);

        static bool IsInternal(string? host)
        {
            if (string.IsNullOrEmpty(host))
			{
				return false;
			}

			host = host.ToLowerInvariant();
            return host == "scan-manga.com" || host.EndsWith(".scan-manga.com");
        }

        void HandleError(NSError error)
        {
            // On ignore les erreurs d'annulation (quand on ouvre un lien externe par exemple)
            if (error.Domain == "NSURLErrorDomain" && error.Code == -999)
			{
				return;
			}

			var errorInfo = WebErrorHelper.GetErrorMessage((int)error.Code);

            customWebView.RaiseError(errorInfo.Title, errorInfo.Message);
        }
    }
}
