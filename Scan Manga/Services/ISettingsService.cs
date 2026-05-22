namespace Scan_Manga.Services;

public interface ISettingsService
{
    AppTheme GetAppTheme();
    void SetAppTheme(AppTheme theme);

    FullScreenMode GetFullScreenMode();
    void SetFullScreenMode(FullScreenMode mode);

    KeepScreenOnMode GetKeepScreenOnMode();
    void SetKeepScreenOnMode(KeepScreenOnMode mode);

    bool IsAdBlockerEnabled();
    void SetAdBlockerEnabled(bool enabled);

    bool LoadLastUrlOnStartup();
    void SetLoadLastUrlOnStartup(bool enabled);

    bool IsHistoryEnabled();
    void SetHistoryEnabled(bool enabled);

    string GetLastUrl();
    void SetLastUrl(string url);

    HashSet<string> GetVisitedUrls();
    void SetVisitedUrls(HashSet<string> urls);
    void ClearVisitedUrls();
}
