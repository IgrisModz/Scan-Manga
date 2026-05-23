using Scan_Manga.Services;

namespace Scan_Manga;

public partial class App : Application
{
    const double width = 1366;
    const double height = 768;

    public App(ISettingsService settingsService)
    {
        InitializeComponent();

        UserAppTheme = settingsService.GetAppTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            MinimumWidth = 1280,
            MinimumHeight = 720,
            Width = width,
            Height = height,
            X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - width) / 2,
            Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density - height) / 2
        };
    }
}