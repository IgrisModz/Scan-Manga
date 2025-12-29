using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiIcons.Material;
using Scan_Manga.Models;

namespace Scan_Manga.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public List<SelectOption> ThemeOptions { get; } =
    [
        new SelectOption { Label = "Système", Icon = MaterialIcons.SettingsSuggest},
        new SelectOption { Label = "Clair", Icon = MaterialIcons.DarkMode},
        new SelectOption { Label = "Sombre", Icon = MaterialIcons.LightMode }
    ];

    public List<SelectOption> FullScreenOptions { get; } =
    [
        new SelectOption { Label = "Lecture uniquement", Icon = MaterialIcons.AutoStories },
        new SelectOption { Label = "Toutes les pages", Icon = MaterialIcons.Fullscreen },
        new SelectOption { Label = "Désactivé", Icon = MaterialIcons.FullscreenExit }
    ];

    [ObservableProperty] private SelectOption _selectedTheme;
    [ObservableProperty] private SelectOption _selectedFullScreenMode;
    [ObservableProperty] private bool _keepScreenOn;
    [ObservableProperty] private bool _isAdBlockerEnabled;
    [ObservableProperty] private bool _loadLastPageOnStartup;
    [ObservableProperty] private bool _isHistoryEnabled;

    [ObservableProperty] private bool isOverlayVisible;
    [ObservableProperty] private string? _overlayTitle;
    [ObservableProperty] private string? _overlayMessage;
    [ObservableProperty] private string? _overlayConfirmText;
    [ObservableProperty] private string? _overlayCancelText;

    private enum OverlayContext { None, ClearHistory, Help }
    private OverlayContext _currentContext = OverlayContext.None;

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
        _currentContext = OverlayContext.ClearHistory;
        OverlayTitle = "Historique";
        OverlayMessage = "Effacer les données locales ?";
        OverlayConfirmText = "Oui";
        OverlayCancelText = "Non";
        IsOverlayVisible = true;
    }

    [RelayCommand]
    private async Task ShowRestoreHelp()
    {
        _currentContext = OverlayContext.Help;
        OverlayTitle = "Reprendre la lecture";
        OverlayMessage = "Si activé, l'application mémorisera votre dernière page visitée...";
        OverlayConfirmText = "Ok";
        OverlayCancelText = string.Empty; // Cache le bouton annuler
        IsOverlayVisible = true;
    }

    [RelayCommand]
    private void OnOverlayResult(bool confirmed) // Paramètre explicite
    {
        IsOverlayVisible = false;

        if (!confirmed)
        {
            _currentContext = OverlayContext.None;
            return;
        }

        switch (_currentContext)
        {
            case OverlayContext.ClearHistory:
                Preferences.Remove("VisitedLinks");
                _currentContext = OverlayContext.None;
                ShowSuccessAlert("L'historique a été vidé.");
                break;
            case OverlayContext.Help:
                _currentContext = OverlayContext.None;
                break;
        }
    }

    private void ShowSuccessAlert(string message)
    {
        OverlayTitle = "Succès";
        OverlayMessage = message;
        OverlayConfirmText = "OK";
        OverlayCancelText = string.Empty;
        IsOverlayVisible = true;
    }
}
