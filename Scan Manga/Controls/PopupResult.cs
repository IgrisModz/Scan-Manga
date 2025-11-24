namespace Scan_Manga.Controls;

public class PopupResult<T>(T value, bool dismissed)
{
    public bool WasDismissedByTappingOutsideOfPopup { get; set; } = dismissed;
    public T Value { get; set; } = value;
}
