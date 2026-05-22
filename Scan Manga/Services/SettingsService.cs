using Scan_Manga.Constants;
using Scan_Manga.Controls;
using Scan_Manga.ViewModels;
using System.Text.Json;

namespace Scan_Manga.Services;

public class SettingsService : ISettingsService
{
    public AppTheme GetAppTheme()
    {
        var themeString = Preferences.Get(PreferenceKeys.SelectedAppTheme, AppTheme.Unspecified.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme : AppTheme.Unspecified;
    }

    public void SetAppTheme(AppTheme theme)
    {
        Preferences.Set(PreferenceKeys.SelectedAppTheme, theme.ToString());
    }

    public FullScreenMode GetFullScreenMode()
    {
        var modeString = Preferences.Get(PreferenceKeys.SelectedFullScreenMode, FullScreenMode.Disabled.ToString());
        return Enum.TryParse<FullScreenMode>(modeString, out var mode) ? mode : FullScreenMode.Disabled;
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

    public bool LoadLastUrlOnStartup()
    {
        return Preferences.Get(PreferenceKeys.LoadLastUrlOnStartup, true);
    }

    public void SetLoadLastUrlOnStartup(bool enabled)
    {
        Preferences.Set(PreferenceKeys.LoadLastUrlOnStartup, enabled);
    }

    public bool IsHistoryEnabled()
    {
        return Preferences.Get(PreferenceKeys.IsHistoryEnabled, true);
    }

    public void SetHistoryEnabled(bool enabled)
    {
        Preferences.Set(PreferenceKeys.IsHistoryEnabled, enabled);
    }

    public string GetLastUrl()
    {
        return Preferences.Get(PreferenceKeys.LastUrl, CustomWebView.DefaultUrl);
    }

    public void SetLastUrl(string url)
    {
        Preferences.Set(PreferenceKeys.LastUrl, url);
    }

    public HashSet<string> GetVisitedUrls()
    {
        var saved = Preferences.Get(PreferenceKeys.VisitedUrls, string.Empty);
        return string.IsNullOrEmpty(saved)
            ? []
            : JsonSerializer.Deserialize<HashSet<string>>(saved) ?? [];
    }

    public void SetVisitedUrls(HashSet<string> urls)
    {
        Preferences.Set(PreferenceKeys.VisitedUrls, JsonSerializer.Serialize(urls));
    }

    public void ClearVisitedUrls()
    {
        Preferences.Remove(PreferenceKeys.VisitedUrls);
    }
}
