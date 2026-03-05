using CommunityToolkit.Mvvm.ComponentModel;
using MauiIcons.MaterialSymbols.Rounded;

namespace Scan_Manga.Models;

public partial class SelectOption : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public MaterialSymbolsRoundedIcons? Icon { get; set; }
    public bool HasIcon => Icon != null;

    [ObservableProperty]
    private bool _isSelected;
}
