using Foundation;
using WebKit;
using Microsoft.Maui.Handlers;
using Scan_Manga.Controls;

namespace Scan_Manga.Platforms.iOS;

public class CustomWebViewHandler : WebViewHandler
{
    IDisposable? _progressObserver;

    public static IPropertyMapper<CustomWebView, CustomWebViewHandler> CustomMapper =
        new PropertyMapper<CustomWebView, CustomWebViewHandler>(Mapper);

    public static CommandMapper<CustomWebView, CustomWebViewHandler> CustomCommands =
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
                    (NSObservedChange change) =>
                    {
                        if (view is CustomWebView custom)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                custom.Progress = handler.PlatformView.EstimatedProgress;
                            });
                        }
                    });
            }
        });
    }

    protected override void DisconnectHandler(WKWebView platformView)
    {
        if (_progressObserver != null)
        {
            // Dispose de l'observer
            _progressObserver?.Dispose();
            _progressObserver = null;
        }
        base.DisconnectHandler(platformView);
    }
}
