using CommunityToolkit.Maui.Core;
using Scan_Manga.Services;
using CommunityToolkit.Maui.Core.Platform;

#if ANDROID
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
#endif

namespace Scan_Manga.Controls;

public class InfoPageBase(IFullScreenService fullScreenService) : ContentPage
{
    private readonly IFullScreenService _fullScreenService = fullScreenService;

    public static readonly BindableProperty CustomStatusBarColorProperty =
        BindableProperty.Create(nameof(CustomStatusBarColor), typeof(Color), typeof(InfoPageBase), null);

    public Color CustomStatusBarColor
    {
        get => (Color)GetValue(CustomStatusBarColorProperty);
        set => SetValue(CustomStatusBarColorProperty, value);
    }

    public InfoPageBase() : this(ServiceHelper.Services.GetRequiredService<IFullScreenService>())
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Application.Current?.RequestedThemeChanged += OnThemeChanged;
        UpdateSystemBars();

        _fullScreenService.ExitFullScreen();
    }

    protected override void OnDisappearing()
    {
        Application.Current!.RequestedThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => UpdateSystemBars();

    private void UpdateSystemBars()
    {
        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
        Color barColor;
        if (CustomStatusBarColor != null)
        {
            barColor = CustomStatusBarColor;
        }
        else
        {
            var colorKey = isDarkMode ? "OffBlack" : "White";
            barColor = Application.Current?.Resources.TryGetValue(colorKey, out var res) == true ? (Color)res : (isDarkMode ? Color.FromArgb("#1F1F1F") : Colors.White);
        }

        var style = barColor.GetLuminosity() > 0.5
                    ? (int)NavigationBarStyle.DarkContent
                    : (int)NavigationBarStyle.LightContent;

        // Update Navigation bar style and color
#if ANDROID
        On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().SetColor(barColor);
        On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().SetStyle((NavigationBarStyle)style);
#endif

        if (OperatingSystem.IsAndroidVersionAtLeast(23) || OperatingSystem.IsIOSVersionAtLeast(15))
        {
            // Update Status bar style and color
            StatusBar.SetColor(barColor);
            StatusBar.SetStyle((StatusBarStyle)style);
        }
    }
}
