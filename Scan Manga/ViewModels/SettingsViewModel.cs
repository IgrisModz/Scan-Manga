using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Constants;
using Scan_Manga.Models;
using Scan_Manga.Services;

namespace Scan_Manga.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    enum OverlayContext { None, ClearHistory, Help }
    OverlayContext currentContext = OverlayContext.None;

    readonly ISettingsService settingsService;

    public List<SelectOption> ThemeOptions { get; } =
    [
        new("Système", MaterialSymbolsRoundedIcons.SettingsSuggest, AppTheme.Unspecified),
        new("Clair", MaterialSymbolsRoundedIcons.LightMode, AppTheme.Light),
        new("Sombre", MaterialSymbolsRoundedIcons.DarkMode, AppTheme.Dark)
    ];

    public List<SelectOption> FullScreenOptions { get; } =
    [
        new("Lecture uniquement", MaterialSymbolsRoundedIcons.AutoStories, FullScreenMode.ReadingOnly),
        new("Toutes les pages", MaterialSymbolsRoundedIcons.Fullscreen, FullScreenMode.AllPages),
        new("Désactivé", MaterialSymbolsRoundedIcons.FullscreenExit, FullScreenMode.Disabled)
    ];

    public List<SelectOption> KeepScreenOnOptions { get; } =
    [
        new("Toutes les pages", MaterialSymbolsRoundedIcons.ScreenLockPortrait, KeepScreenOnMode.AllPages),
        new("Lecture uniquement", MaterialSymbolsRoundedIcons.MenuBook, KeepScreenOnMode.ReadingOnly),
        new("Charge uniquement (Lecture)", MaterialSymbolsRoundedIcons.BatteryChargingFull, KeepScreenOnMode.ChargingOnly),
        new("Désactivé", MaterialSymbolsRoundedIcons.ScreenLockRotation, KeepScreenOnMode.Disabled)
    ];

    [ObservableProperty] public partial SelectOption SelectedAppTheme { get; set; }

    [ObservableProperty] public partial SelectOption SelectedFullScreenMode { get; set; }
    [ObservableProperty] public partial SelectOption SelectedKeepScreenOnMode { get; set; }
    [ObservableProperty] public partial bool IsAdBlockerEnabled { get; set; }
    [ObservableProperty] public partial bool LoadLastUrlOnStartup { get; set; }
    [ObservableProperty] public partial bool IsHistoryEnabled { get; set; }

    [ObservableProperty] public partial Overlay? Overlay { get; set; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        this.settingsService = settingsService;

        var savedTheme = settingsService.GetAppTheme();
        SelectedAppTheme = ThemeOptions.First(t => (AppTheme)t.Value == savedTheme);

        var savedFullScreenMode = settingsService.GetFullScreenMode();
        SelectedFullScreenMode = FullScreenOptions.First(m => (FullScreenMode)m.Value == savedFullScreenMode);

        var savedKeepScreenOnMode = settingsService.GetKeepScreenOnMode();
        SelectedKeepScreenOnMode = KeepScreenOnOptions.First(m => (KeepScreenOnMode)m.Value == savedKeepScreenOnMode);

        IsAdBlockerEnabled = settingsService.IsAdBlockerEnabled();
        LoadLastUrlOnStartup = settingsService.LoadLastUrlOnStartup();
        IsHistoryEnabled = settingsService.IsHistoryEnabled();
    }

    partial void OnSelectedAppThemeChanged(SelectOption value)
    {
        var theme = (AppTheme)value.Value;
        settingsService.SetAppTheme(theme);
        Application.Current?.UserAppTheme = theme;
    }

    partial void OnSelectedFullScreenModeChanged(SelectOption value) => settingsService.SetFullScreenMode((FullScreenMode)value.Value);
    partial void OnSelectedKeepScreenOnModeChanged(SelectOption value) => settingsService.SetKeepScreenOnMode((KeepScreenOnMode)value.Value);
    partial void OnIsAdBlockerEnabledChanged(bool value) => settingsService.SetAdBlockerEnabled(value);
    partial void OnLoadLastUrlOnStartupChanged(bool value) => settingsService.SetLoadLastUrlOnStartup(value);
    partial void OnIsHistoryEnabledChanged(bool value) => settingsService.SetHistoryEnabled(value);

    [RelayCommand]
    static async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task ClearHistory()
    {
        currentContext = OverlayContext.ClearHistory;
        Overlay = new Overlay
        {
            Title = "Historique",
            Message = "Effacer les données locales ?",
            ConfirmText = "Oui",
            CancelText = "Non",
            IsVisible = true
        };
    }

    [RelayCommand]
    async Task ShowRestoreHelp()
    {
        currentContext = OverlayContext.Help;
        Overlay = new Overlay
        {
            Title = "Reprendre la lecture",
            Message = "Si activé, l'application mémorisera votre dernière page visitée...",
            ConfirmText = "Ok",
            CancelText = string.Empty, // Cache le bouton annuler
            IsVisible = true
        };
    }

    [RelayCommand]
    void OverlayResult(bool confirmed) // Paramètre explicite
    {
        Overlay?.IsVisible = false;

        if (!confirmed)
        {
            currentContext = OverlayContext.None;
            return;
        }

        switch (currentContext)
        {
            case OverlayContext.ClearHistory:
                Preferences.Remove(PreferenceKeys.VisitedUrls);
                currentContext = OverlayContext.None;
                ShowSuccessAlert("L'historique a été vidé.");
                break;
            case OverlayContext.Help:
                currentContext = OverlayContext.None;
                break;
        }
    }

    void ShowSuccessAlert(string message)
    {
        Overlay = new Overlay
        {
            Title = "Succès",
            Message = message,
            ConfirmText = "OK",
            CancelText = string.Empty,
            IsVisible = true
        };
    }
}
