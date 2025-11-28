using Scan_Manga.Pages;

namespace Scan_Manga;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LegalNoticesPage), typeof(LegalNoticesPage));
        Routing.RegisterRoute(nameof(PrivacyPolicyPage), typeof(PrivacyPolicyPage));
        Routing.RegisterRoute(nameof(TermsOfUsePage), typeof(TermsOfUsePage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(DonatePage), typeof(DonatePage));
    }
}
