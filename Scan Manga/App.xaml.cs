using Scan_Manga.Services;

namespace Scan_Manga;

public partial class App : Application
{
    public const double Width = 1366;
    public const double Height = 768;

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
            Width = Width,
            Height = Height,
            X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - Width) / 2,
            Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density - Height) / 2
        };
    }
}