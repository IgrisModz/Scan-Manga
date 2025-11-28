using Scan_Manga.Controls;

namespace Scan_Manga.Pages;

public partial class DonatePage : InfoPageBase
{
    public DonatePage()
	{
		InitializeComponent();
    }

    private void OnPaypalClicked(object sender, EventArgs e)
	{
		Browser.OpenAsync("https://www.paypal.com/donate/?hosted_button_id=6C4SA7HAWZHGU");
    }

    private void OnPatreonClicked(object sender, EventArgs e)
    {
        Browser.OpenAsync("https://patreon.com/IgrisModz");
    }

    private void OnKofiClicked(object sender, EventArgs e)
    {
        Browser.OpenAsync("https://Ko-fi.com/igrismodz");
    }
}