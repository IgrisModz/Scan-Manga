using Scan_Manga.Services;

namespace Scan_Manga;

public partial class App : Application
{
    private readonly AppShell _appShell;

    public App(AppShell appShell, ISettingsService settingsService)
    {
        InitializeComponent();

        _appShell = appShell;

        UserAppTheme = settingsService.GetTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
    }
}