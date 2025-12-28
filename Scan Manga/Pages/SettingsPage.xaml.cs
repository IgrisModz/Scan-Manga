using Scan_Manga.Controls;
using Scan_Manga.ViewModels;

namespace Scan_Manga.Pages;

public partial class SettingsPage : PageBase
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}
}