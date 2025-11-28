using CommunityToolkit.Maui;
using MauiIcons.Material;
using MauiIcons.FontAwesome.Brand;
using Scan_Manga.Controls;
using Scan_Manga.Services;

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
                .UseMaterialMauiIcons()
                .UseFontAwesomeBrandMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<CustomWebView, CustomWebViewHandler>();
#elif IOS
                handlers.AddHandler<CustomWebView, CustomWebViewHandler>();
#endif
                });

#if ANDROID
            builder.Services.AddSingleton<IFullScreenService, AndroidFullscreenService>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        ServiceHelper.Services = app.Services;

        return app;
    }
}
