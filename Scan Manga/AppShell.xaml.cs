using Scan_Manga.Pages;
using Scan_Manga.Services;

namespace Scan_Manga;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            ContentTemplate = new DataTemplate(() => ServiceHelper.Services.GetRequiredService<MainPage>()),
            Route = "MainPage"
        });

        Routing.RegisterRoute(nameof(LegalNoticesPage), typeof(LegalNoticesPage));
        Routing.RegisterRoute(nameof(PrivacyPolicyPage), typeof(PrivacyPolicyPage));
        Routing.RegisterRoute(nameof(TermsOfUsePage), typeof(TermsOfUsePage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(DonatePage), typeof(DonatePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
