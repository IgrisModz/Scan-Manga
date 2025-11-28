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
            PopupBorder.FadeToAsync(1, 250, Easing.CubicInOut),
            PopupBorder.ScaleToAsync(1, 250, Easing.CubicOut)
        );

		foreach (var btn in ContentStack.Children.OfType<Button>())
        {
            AnimateButtonEntry(btn);
        }
    }

    private static async void AnimateButtonEntry(Button btn)
    {
        await Task.Delay(Random.Shared.Next(50, 150)); // léger décalage aléatoire pour effet naturel
        await Task.WhenAll(
            btn.FadeToAsync(1, 300, Easing.CubicInOut),
            btn.ScaleToAsync(1, 300, Easing.CubicOut)
        );
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

    private async Task OnClose()
    {
        foreach (var btn in ContentStack.Children.OfType<Button>())
        {
            _ = await btn.FadeToAsync(0, 150);
        }

        await Task.WhenAll(
                PopupBorder.FadeToAsync(0, 250),
                PopupBorder.ScaleToAsync(0.8, 250)
            );
    }
}