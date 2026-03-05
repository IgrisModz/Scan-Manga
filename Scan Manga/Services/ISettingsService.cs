using Scan_Manga.ViewModels;

namespace Scan_Manga.Services;

public interface ISettingsService
{
    AppTheme GetTheme();
    void SetTheme(AppTheme theme);

    FullScreenMode GetFullScreenMode();
    void SetFullScreenMode(FullScreenMode mode);

    KeepScreenOnMode GetKeepScreenOnMode();
    void SetKeepScreenOnMode(KeepScreenOnMode mode);

    bool IsAdBlockerEnabled();
    void SetAdBlockerEnabled(bool enabled);

    bool LoadLastPageOnStartup();
    void SetLoadLastPageOnStartup(bool enabled);

    bool IsHistoryEnabled();
    void SetHistoryEnabled(bool enabled);

    string GetLastUrl(string defaultUrl);
    void SetLastUrl(string url);

    HashSet<string> GetVisitedLinks();
    void SetVisitedLinks(HashSet<string> links);
    void ClearVisitedLinks();
}
