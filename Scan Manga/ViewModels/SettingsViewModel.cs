using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiIcons.Material;
using Scan_Manga.Models;

namespace Scan_Manga.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private List<SelectOption> _themeOptions =
    [
        new SelectOption { Label = "Système", Icon = MaterialIcons.SettingsSuggest},
        new SelectOption { Label = "Clair", Icon = MaterialIcons.DarkMode},
        new SelectOption { Label = "Sombre", Icon = MaterialIcons.LightMode }
    ];

    [ObservableProperty]
    private List<SelectOption> _fullScreenOptions =
    [
        new SelectOption { Label = "Lecture uniquement", Icon = MaterialIcons.AutoStories },
        new SelectOption { Label = "Toutes les pages", Icon = MaterialIcons.Fullscreen },
        new SelectOption { Label = "Désactivé", Icon = MaterialIcons.FullscreenExit }
    ];

    [ObservableProperty]
    private SelectOption _selectedTheme;

    [ObservableProperty]
    private SelectOption _selectedFullScreenMode;

    [ObservableProperty]
    private bool _keepScreenOn;

    [ObservableProperty]
    private bool _isAdBlockerEnabled;

    [ObservableProperty]
    private bool _loadLastPageOnStartup;

    [ObservableProperty]
    private bool _isHistoryEnabled;

    public SettingsViewModel()
    {
        SelectedTheme = ThemeOptions.First(t => t.Label == Preferences.Get("AppTheme", "Système"));
        SelectedFullScreenMode = FullScreenOptions.First(m => m.Label == Preferences.Get("FullScreenMode", "Lecture uniquement"));
        KeepScreenOn = Preferences.Get("KeepScreenOn", true);
        IsAdBlockerEnabled = Preferences.Get("IsAdBlockerEnabled", true);
        LoadLastPageOnStartup = Preferences.Get("LoadLastPageOnStartup", true);
        IsHistoryEnabled = Preferences.Get("IsHistoryEnabled", true);
    }

    partial void OnSelectedThemeChanged(SelectOption value)
    {
        Preferences.Set("AppTheme", value.Label);
        Application.Current?.UserAppTheme = value.Label switch
        {
            "Clair" => AppTheme.Light,
            "Sombre" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    partial void OnSelectedFullScreenModeChanged(SelectOption value) => Preferences.Set("FullScreenMode", value.Label);

    partial void OnKeepScreenOnChanged(bool value) => Preferences.Set("KeepScreenOn", value);

    partial void OnIsAdBlockerEnabledChanged(bool value) => Preferences.Set("IsAdBlockerEnabled", value);

    partial void OnLoadLastPageOnStartupChanged(bool value) => Preferences.Set("LoadLastPageOnStartup", value);

    partial void OnIsHistoryEnabledChanged(bool value) => Preferences.Set("IsHistoryEnabled", value);

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task ClearHistory()
    {
#if NET10_0_OR_GREATER
        bool confirm = await Shell.Current.DisplayAlertAsync("Historique", "Effacer les données locales ?", "Oui", "Non");
#else
        bool confirm = await Shell.Current.DisplayAlert("Historique", "Effacer les données locales ?", "Oui", "Non");
#endif
        if (confirm)
        {
            Preferences.Remove("VisitedLinks");

#if NET10_0_OR_GREATER
            await Shell.Current.DisplayAlertAsync("Succès", "L'historique a été vidé.", "OK");
#else
            await Shell.Current.DisplayAlert("Succès", "L'historique a été vidé.", "OK");
#endif
        }
    }

    [RelayCommand]
    private async Task ShowRestoreHelp()
    {
#if NET10_0_OR_GREATER
        await Shell.Current.DisplayAlertAsync("Reprendre la lecture",
            "Si activé, l'application mémorisera votre dernière page visitée et la rechargera automatiquement au prochain lancement.",
            "Ok");
#else
        await Shell.Current.DisplayAlert("Reprendre la lecture",
            "Si activé, l'application mémorisera votre dernière page visitée et la rechargera automatiquement au prochain lancement.",
            "Ok");
#endif
    }
}
