using Scan_Manga.Services;

namespace Scan_Manga;

public partial class App : Application
{
    public App(ISettingsService settingsService)
    {
        InitializeComponent();

        UserAppTheme = settingsService.GetAppTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

#if WINDOWS
        const double width = 1366;
        const double height = 768;

        window.MinimumWidth = 1280;
        window.MinimumHeight = 720;
        window.Width = width;
        window.Height = height;
        window.X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - width) / 2;
        window.Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density - height) / 2;
#endif

        return window;
    }
}