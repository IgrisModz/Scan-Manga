using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiIcons.Material;
using Scan_Manga.Helpers;
using Scan_Manga.Models;
using Scan_Manga.Services;

namespace Scan_Manga.ViewModels;

public enum FullScreenMode
{
    ReadingOnly,
    AllPages,
    Disabled
}

public enum KeepScreenOnMode
{
    AllPages,
    ReadingOnly,
    ChargingOnly,
    Disabled
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public List<SelectOption> ThemeOptions { get; } =
    [
        new() { Label = "Système", Icon = MaterialIcons.SettingsSuggest, Value = AppTheme.Unspecified },
        new() { Label = "Clair", Icon = MaterialIcons.DarkMode, Value = AppTheme.Light },
        new() { Label = "Sombre", Icon = MaterialIcons.LightMode, Value = AppTheme.Dark }
    ];

    public List<SelectOption> FullScreenOptions { get; } =
    [
        new() { Label = "Lecture uniquement", Icon = MaterialIcons.AutoStories, Value = FullScreenMode.ReadingOnly },
        new() { Label = "Toutes les pages", Icon = MaterialIcons.Fullscreen, Value = FullScreenMode.AllPages },
        new() { Label = "Désactivé", Icon = MaterialIcons.FullscreenExit, Value = FullScreenMode.Disabled }
    ];

    public List<SelectOption> KeepScreenOnOptions { get; } =
    [
        new() { Label = "Toutes les pages", Icon = MaterialIcons.ScreenLockPortrait, Value = KeepScreenOnMode.AllPages },
        new() { Label = "Lecture uniquement", Icon = MaterialIcons.MenuBook, Value = KeepScreenOnMode.ReadingOnly },
        new() { Label = "Charge uniquement (Lecture)", Icon = MaterialIcons.BatteryChargingFull, Value = KeepScreenOnMode.ChargingOnly },
        new() { Label = "Désactivé", Icon = MaterialIcons.ScreenLockRotation, Value = KeepScreenOnMode.Disabled }
    ];

    [ObservableProperty] private SelectOption _selectedTheme;
    [ObservableProperty] private SelectOption _selectedFullScreenMode;
    [ObservableProperty] private SelectOption _selectedKeepScreenOnMode;
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

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var savedTheme = _settingsService.GetTheme();
        SelectedTheme = ThemeOptions.First(t => (AppTheme)t.Value == savedTheme);

        var savedFullScreenMode = _settingsService.GetFullScreenMode();
        SelectedFullScreenMode = FullScreenOptions.First(m => (FullScreenMode)m.Value == savedFullScreenMode);

        var savedKeepScreenOnMode = _settingsService.GetKeepScreenOnMode();
        SelectedKeepScreenOnMode = KeepScreenOnOptions.First(m => (KeepScreenOnMode)m.Value == savedKeepScreenOnMode);

        IsAdBlockerEnabled = _settingsService.IsAdBlockerEnabled();
        LoadLastPageOnStartup = _settingsService.LoadLastPageOnStartup();
        IsHistoryEnabled = _settingsService.IsHistoryEnabled();
    }

    partial void OnSelectedThemeChanged(SelectOption value)
    {
        var theme = (AppTheme)value.Value;
        _settingsService.SetTheme(theme);
        Application.Current?.UserAppTheme = theme;
    }

    partial void OnSelectedFullScreenModeChanged(SelectOption value) => _settingsService.SetFullScreenMode((FullScreenMode)value.Value);
    partial void OnSelectedKeepScreenOnModeChanged(SelectOption value) => _settingsService.SetKeepScreenOnMode((KeepScreenOnMode)value.Value);
    partial void OnIsAdBlockerEnabledChanged(bool value) => _settingsService.SetAdBlockerEnabled(value);
    partial void OnLoadLastPageOnStartupChanged(bool value) => _settingsService.SetLoadLastPageOnStartup(value);
    partial void OnIsHistoryEnabledChanged(bool value) => _settingsService.SetHistoryEnabled(value);

    [RelayCommand]
    private async Task GoBack(VerticalStackLayout sender)
    {
        await sender.ScaleToSafe(0.7, 100);
        await sender.ScaleToSafe(1, 100);

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ClearHistory(VerticalStackLayout sender)
    {
        await sender.ScaleToSafe(0.7, 100);
        await sender.ScaleToSafe(1, 100);

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
    private void OverlayResult(bool confirmed) // Paramètre explicite
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
