using Microsoft.Maui.Handlers;

namespace Scan_Manga.Handlers;

public class SelectableWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(Android.Webkit.WebView platformView)
    {
        base.ConnectHandler(platformView);

        platformView.Settings.JavaScriptEnabled = true;
        platformView.Settings.DomStorageEnabled = true;

        platformView.LongClickable = true;
        platformView.SetOnLongClickListener(null);
    }
}
