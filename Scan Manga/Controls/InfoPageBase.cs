using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Scan_Manga.Services;

namespace Scan_Manga.Controls;

public class InfoPageBase : ContentPage
{
    private readonly IFullScreenService _fullScreenService;

    public InfoPageBase() : this(ServiceHelper.Services.GetRequiredService<IFullScreenService>())
    {
    }

    public InfoPageBase(IFullScreenService fullScreenService)
    {
        _fullScreenService = fullScreenService;

        if (OperatingSystem.IsAndroidVersionAtLeast(23) || OperatingSystem.IsIOSVersionAtLeast(15))
        {
            var statusBarBehavior = new StatusBarBehavior { StatusBarStyle = StatusBarStyle.Default };

            var lightColor = Colors.White;
            if (Application.Current?.Resources.TryGetValue("White", out var whiteObj) == true && whiteObj is Color whiteColor)
            {
                lightColor = whiteColor;
            }

            var darkColor = Color.FromArgb("#1F1F1F");
            if (Application.Current?.Resources.TryGetValue("OffBlack", out var offBlackObj) == true && offBlackObj is Color offBlackColor)
            {
                darkColor = offBlackColor;
            }

            statusBarBehavior.SetAppThemeColor(
                StatusBarBehavior.StatusBarColorProperty,
                lightColor,
                darkColor
            );
            this.Behaviors.Add(statusBarBehavior);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _fullScreenService.ExitFullScreen();
    }
}
