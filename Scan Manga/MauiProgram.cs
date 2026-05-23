using CommunityToolkit.Maui;
using MauiIcons.FontAwesome.Brands;
using MauiIcons.MaterialSymbols.Rounded;
using Plugin.DeviceCharging;
using Scan_Manga.Services;
using Scan_Manga.Pages;
using Scan_Manga.ViewModels;

#if ANDROID || IOS
using Scan_Manga.Controls;
using Microsoft.Maui.Handlers;
#endif

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
#if ANDROID || IOS
                handlers.AddHandler<CustomWebView, CustomWebViewHandler>();
                handlers.AddHandler<Layout, LayoutHandler>();
#endif
            });

        builder.Services.AddDeviceCharging();

        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddSingleton<AboutPage>();
        builder.Services.AddSingleton<DonatePage>();
        builder.Services.AddSingleton<LegalNoticesPage>();
        builder.Services.AddSingleton<PrivacyPolicyPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<TermsOfUsePage>();

        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}