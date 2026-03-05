using System.Text.Json;
using Scan_Manga.Constants;
using Scan_Manga.ViewModels;

namespace Scan_Manga.Services;

public class SettingsService : ISettingsService
{
    public AppTheme GetTheme()
    {
        var themeString = Preferences.Get(PreferenceKeys.SelectedTheme, AppTheme.Unspecified.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme : AppTheme.Unspecified;
    }

    public void SetTheme(AppTheme theme)
    {
        Preferences.Set(PreferenceKeys.SelectedTheme, theme.ToString());
    }

    public FullScreenMode GetFullScreenMode()
    {
        var modeString = Preferences.Get(PreferenceKeys.SelectedFullScreenMode, FullScreenMode.ReadingOnly.ToString());
        return Enum.TryParse<FullScreenMode>(modeString, out var mode) ? mode : FullScreenMode.ReadingOnly;
    }

    public void SetFullScreenMode(FullScreenMode mode)
    {
        Preferences.Set(PreferenceKeys.SelectedFullScreenMode, mode.ToString());
    }

    public KeepScreenOnMode GetKeepScreenOnMode()
    {
        var modeString = Preferences.Get(PreferenceKeys.SelectedKeepScreenOnMode, KeepScreenOnMode.Disabled.ToString());
        return Enum.TryParse<KeepScreenOnMode>(modeString, out var mode) ? mode : KeepScreenOnMode.Disabled;
    }

    public void SetKeepScreenOnMode(KeepScreenOnMode mode)
    {
        Preferences.Set(PreferenceKeys.SelectedKeepScreenOnMode, mode.ToString());
    }

    public bool IsAdBlockerEnabled()
    {
        return Preferences.Get(PreferenceKeys.IsAdBlockerEnabled, true);
    }

    public void SetAdBlockerEnabled(bool enabled)
    {
        Preferences.Set(PreferenceKeys.IsAdBlockerEnabled, enabled);
    }

    public bool LoadLastPageOnStartup()
    {
        return Preferences.Get(PreferenceKeys.LoadLastPageOnStartup, true);
    }

    public void SetLoadLastPageOnStartup(bool enabled)
    {
        Preferences.Set(PreferenceKeys.LoadLastPageOnStartup, enabled);
    }

    public bool IsHistoryEnabled()
    {
        return Preferences.Get(PreferenceKeys.IsHistoryEnabled, true);
    }

    public void SetHistoryEnabled(bool enabled)
    {
        Preferences.Set(PreferenceKeys.IsHistoryEnabled, enabled);
    }

    public string GetLastUrl(string defaultUrl)
    {
        return Preferences.Get(PreferenceKeys.LastUrl, defaultUrl);
    }

    public void SetLastUrl(string url)
    {
        Preferences.Set(PreferenceKeys.LastUrl, url);
    }

    public HashSet<string> GetVisitedLinks()
    {
        var saved = Preferences.Get(PreferenceKeys.VisitedLinks, string.Empty);
        return string.IsNullOrEmpty(saved)
            ? []
            : JsonSerializer.Deserialize<HashSet<string>>(saved) ?? [];
    }

    public void SetVisitedLinks(HashSet<string> links)
    {
        Preferences.Set(PreferenceKeys.VisitedLinks, JsonSerializer.Serialize(links));
    }

    public void ClearVisitedLinks()
    {
        Preferences.Remove(PreferenceKeys.VisitedLinks);
    }
}
