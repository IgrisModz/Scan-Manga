namespace Scan_Manga;

public partial class App : Application
{
    private readonly AppShell _appShell;
    public App(AppShell appShell)
    {
        InitializeComponent();

        _appShell = appShell;

        var savedTheme = Preferences.Get("SelectedTheme", "Système");
        UserAppTheme = savedTheme switch
        {
            "Clair" => AppTheme.Light,
            "Sombre" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
    }
}