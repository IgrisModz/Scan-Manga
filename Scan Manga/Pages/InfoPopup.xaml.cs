using CommunityToolkit.Maui.Views;

namespace Scan_Manga.Pages;

public partial class InfoPopup : Popup<ILegalPage>
{
	public InfoPopup()
	{
		InitializeComponent();
	}

    private async void OnCloseClicked(object sender, EventArgs e)
        => await CloseAsync();

    private async void OnNoticesClicked(object sender, EventArgs e)
    {
        await CloseAsync(new LegalNoticesPage());
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        await CloseAsync(new PrivacyPolicyPage());
    }

    private async void OnTermClicked(object sender, EventArgs e)
    {
        await CloseAsync(new TermsOfUsePage());
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await CloseAsync(new AboutPage());
    }
}