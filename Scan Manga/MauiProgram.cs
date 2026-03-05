using CommunityToolkit.Maui;
using MauiIcons.FontAwesome.Brands;
using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Controls;
using Scan_Manga.Services;
using Scan_Manga.Pages;
using Scan_Manga.ViewModels;
using Microsoft.Maui.Handlers;

#if ANDROID
using Scan_Manga.Platforms.Android;
#elif IOS
using Scan_Manga.Platforms.iOS;
#endif

#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace Scan_Manga;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseFontAwesomeBrands()
                .UseMaterialSymbolsRounded()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<CustomWebView, CustomWebViewHandler>();
                    handlers.AddHandler<Layout, LayoutHandler>();
                });

        builder.Services.AddSingleton<IFullScreenService, FullscreenService>();
        builder.Services.AddSingleton<IChargingService, ChargingService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddSingleton<AboutPage>();
        builder.Services.AddSingleton<DonatePage>();
        builder.Services.AddSingleton<LegalNoticesPage>();
        builder.Services.AddSingleton<PrivacyPolicyPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<TermsOfUsePage>();

        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        ServiceHelper.Services = app.Services;

        return app;
    }
}
