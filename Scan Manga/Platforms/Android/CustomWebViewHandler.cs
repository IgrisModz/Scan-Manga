using Android.Util;
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;
using Webkit = Android.Webkit;

namespace Scan_Manga.Platforms.Android;

public class CustomWebViewHandler : WebViewHandler
{
    public static IPropertyMapper<CustomWebView, CustomWebViewHandler> CustomMapper =
            new PropertyMapper<CustomWebView, CustomWebViewHandler>(Mapper)
            {
            };

    public static CommandMapper<CustomWebView, CustomWebViewHandler> CustomCommands =
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
}
