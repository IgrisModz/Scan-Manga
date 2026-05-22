using CommunityToolkit.Mvvm.ComponentModel;

namespace Scan_Manga.Models;

public partial class SelectOption(string label, Enum icon, Enum value) : ObservableObject
{
    public string Label { get; init; } = label;
    public Enum Icon { get; init; } = icon;
    public Enum Value { get; init; } = value;

    public bool HasIcon => Icon != null;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
