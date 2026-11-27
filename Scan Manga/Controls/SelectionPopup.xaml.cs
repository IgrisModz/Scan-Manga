using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Scan_Manga.Models;

namespace Scan_Manga.Controls;

public partial class SelectionPopup : Popup<SelectOption>
{
    bool isClosing;

    public SelectionPopup(IList<SelectOption> options, SelectOption? currentSelected)
    {
        InitializeComponent();

        foreach (var item in options)
        {
            item.IsSelected = (item == currentSelected);
        }

        ListOptions.ItemsSource = options;
    }

    async void OnItemTapped(object? sender, TouchGestureCompletedEventArgs e)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = e.TouchCommandParameter is SelectOption;
        if (e.TouchCommandParameter is SelectOption clickedOption)
        {
            // 2. Logique "Single Selection" : On décoche tout le monde sauf celui cliqué
            var allItems = ListOptions.ItemsSource.Cast<SelectOption>();

            foreach (var item in allItems)
            {
                if (item.IsSelected && item != clickedOption)
                {
                    item.IsSelected = false;
                }
            }

            // 3. On active le nouveau
            clickedOption.IsSelected = true;

            // Optionnel : Un petit délai pour que l'utilisateur voie le "flash" bleu avant la fermeture
            await Task.Delay(50).ConfigureAwait(true);

            // 4. On ferme et on renvoie l'unique item sélectionné
            await CloseAsync(clickedOption).ConfigureAwait(true);
        }
    }
}