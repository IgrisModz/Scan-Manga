using MauiIcons.Material;

namespace Scan_Manga.Models;

public class SelectOption
{
    public string Label { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public MaterialIcons? Icon { get; set; } // Utilisation du type spécifique

    public bool HasIcon => Icon != null;
}
