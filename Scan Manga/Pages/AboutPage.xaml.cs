using Scan_Manga.Controls;
using System.Windows.Input;

namespace Scan_Manga.Pages;

public partial class AboutPage : InfoPage
{
    public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

    public AboutPage()
	{
		InitializeComponent();
        BindingContext = this;
    }
}