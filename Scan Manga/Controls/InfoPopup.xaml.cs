using CommunityToolkit.Maui.Views;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class InfoPopup : Popup<string>
{
	public InfoPopup()
	{
        InitializeComponent();

		Opened += OnOpened;
    }

	private async void OnOpened(object? sender, EventArgs e)
	{
        await Task.WhenAll(
#if NET10_OR_GREATER
            PopupBorder.FadeToAsync(1, 250, Easing.CubicInOut),
            PopupBorder.ScaleToAsync(1, 250, Easing.CubicOut)
#elif NET9_0
            PopupBorder.FadeTo(1, 250, Easing.CubicInOut),
            PopupBorder.ScaleTo(1, 250, Easing.CubicOut)
#endif
        );

		foreach (var btn in ContentStack.Children.OfType<Button>())
        {
#if NET10_OR_GREATER
            await btn.FadeToAsync(1, 180, Easing.CubicInOut);
#elif NET9_0
            await btn.FadeTo(1, 180, Easing.CubicInOut);
#endif
        }
    }

    private async void OnNoticesClicked(object sender, EventArgs e)
    {
        await OnClose();
        await CloseAsync(nameof(LegalNoticesPage));
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        await OnClose();
        await CloseAsync(nameof(PrivacyPolicyPage));
    }

    private async void OnTermsClicked(object sender, EventArgs e)
    {
        await OnClose();
        await CloseAsync(nameof(TermsOfUsePage));
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await OnClose();
        await CloseAsync(nameof(AboutPage));
    }

    public async Task OnClose()
    {
        foreach (var btn in ContentStack.Children.OfType<Button>().Reverse())
        {
#if NET10_OR_GREATER
            _ = await btn.FadeToAsync(0, 150);
#elif NET9_0
            _ = await btn.FadeTo(0, 150);
#endif
        }

        await Task.WhenAll(
#if NET10_OR_GREATER
                PopupBorder.FadeToAsync(0, 250),
                PopupBorder.ScaleToAsync(0.8, 250)
#elif NET9_0
                PopupBorder.FadeTo(0, 250),
                PopupBorder.ScaleTo(0.8, 250)
#endif
            );
    }
}