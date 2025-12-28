using CommunityToolkit.Maui.Views;
using Scan_Manga.Models;

namespace Scan_Manga.Controls;

public partial class SelectionPopup : Popup<SelectOption>
{
    public SelectionPopup(IList<SelectOption> options)
    {
        InitializeComponent();
        ListOptions.ItemsSource = options;
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectOption? selected = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as SelectOption : null;
        await CloseAsync(result: selected is not null ? selected : default!);
    }
}