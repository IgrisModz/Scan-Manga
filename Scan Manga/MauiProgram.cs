using CommunityToolkit.Maui;
using Scan_Manga.Services;
using MauiIcons.Material;
using MauiIcons.FontAwesome.Brand;

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
            });

        builder.Services.AddSingleton<ISystemBarsService, SystemBarsService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
