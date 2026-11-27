namespace Scan_Manga.Pages;

public partial class MainPage
{
	partial void ConfigureWebViewContainer()
	{
		WebRefreshView.Content = null;

		// The WebView scrolls its own content; keep it sized to the refresh viewport.
		MainWebView.SetBinding(HeightRequestProperty, new Binding(nameof(Height), source: WebRefreshView));
		WebRefreshView.Content = new ScrollView { Content = MainWebView };
	}
}
