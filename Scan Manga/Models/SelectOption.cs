using CommunityToolkit.Mvvm.ComponentModel;
using MauiIcons.Material;

namespace Scan_Manga.Models;

public partial class SelectOption : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public MaterialIcons? Icon { get; set; }
    public bool HasIcon => Icon != null;

    [ObservableProperty]
    private bool _isSelected;
}
