using CommunityToolkit.Maui;
using Scan_Manga.Services;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls.Shapes;

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
            .UseMauiCommunityToolkit(static options =>
            {
                options.SetPopupDefaults(new DefaultPopupSettings
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
                    BackgroundColor = Colors.Transparent,
                    Margin = 10,
                    Padding = 0
                });

                options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
                    OnTappingOutsideOfPopup = async () => await Toast.Make("Popup Dismissed").Show(CancellationToken.None),
                    PageOverlayColor = Color.FromArgb("#80000000"),
                    Shadow = new Shadow
                    {
                        Brush = Colors.Black,
                        Offset = new Point(10, 10),
                        Opacity = 0.5f,
                        Radius = 25
                    },
                    Shape = new RoundRectangle
                    {
                        CornerRadius = new CornerRadius(20),
                        Stroke = Colors.Gray,
                        StrokeThickness = 1
                    }
                });
            })
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
