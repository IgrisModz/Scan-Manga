using Scan_Manga.Controls;

namespace Scan_Manga.Pages;

public partial class DonatePage : InfoPage
{
    public DonatePage()
	{
		InitializeComponent();
    }

    void OnPaypalClicked(object sender, EventArgs e)
	{
		Browser.OpenAsync("https://www.paypal.com/donate/?hosted_button_id=6C4SA7HAWZHGU");
    }

    void OnPatreonClicked(object sender, EventArgs e)
    {
        Browser.OpenAsync("https://patreon.com/IgrisModz");
    }

    void OnKofiClicked(object sender, EventArgs e)
    {
        Browser.OpenAsync("https://Ko-fi.com/igrismodz");
    }
}