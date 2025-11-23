using System.Windows.Input;

namespace Scan_Manga.Pages;

public partial class AboutPage : ContentPage, ILegalPage
{
    public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

    public AboutPage()
	{
		InitializeComponent();
        BindingContext = this;
    }
}