using CommunityToolkit.Maui.Core;
using Scan_Manga.Services;
using CommunityToolkit.Maui.Core.Platform;

#if ANDROID
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
#endif

namespace Scan_Manga.Controls;

public class InfoPageBase(IFullScreenService fullScreenService) : ContentPage
{
    protected readonly IFullScreenService _fullScreenService = fullScreenService;

    // Couleurs de fallback mises en cache pour éviter le parsing hex à chaque appel
    private static readonly Color DarkFallback = Color.FromArgb("#1F1F1F");
    private static readonly Color LightFallback = Colors.White;

    public static readonly BindableProperty CustomStatusBarColorProperty =
        BindableProperty.Create(nameof(CustomStatusBarColor), typeof(Color), typeof(InfoPageBase), null);

    public static readonly BindableProperty CustomNavigationBarColorProperty =
        BindableProperty.Create(nameof(CustomNavigationBarColor), typeof(Color), typeof(InfoPageBase), null);

    public Color? CustomStatusBarColor
    {
        get => (Color?)GetValue(CustomStatusBarColorProperty);
        set => SetValue(CustomStatusBarColorProperty, value);
    }

    public Color? CustomNavigationBarColor
    {
        get => (Color?)GetValue(CustomNavigationBarColorProperty);
        set => SetValue(CustomNavigationBarColorProperty, value);
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
        Application.Current?.RequestedThemeChanged -= OnThemeChanged;

        base.OnDisappearing();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => UpdateSystemBars();

    private void UpdateSystemBars()
    {
        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        var finalStatusBarColor = GetThemedColor(CustomStatusBarColor, isDarkMode);
        var finalNavBarColor = GetThemedColor(CustomNavigationBarColor, isDarkMode);

        // Update Navigation bar style and color
#if ANDROID
        var navPlatform = On<Microsoft.Maui.Controls.PlatformConfiguration.Android>();
        navPlatform.SetColor(finalNavBarColor);
        // Calcul du style spécifiquement pour la barre de navigation
        navPlatform.SetStyle(GetNavigationBarStyle(finalNavBarColor));
#endif

        if (OperatingSystem.IsAndroidVersionAtLeast(23) || OperatingSystem.IsIOSVersionAtLeast(15))
        {
            // Update Status bar style and color
            StatusBar.SetColor(finalStatusBarColor);
            StatusBar.SetStyle(GetStatusBarStyle(finalStatusBarColor));
        }
    }

    private static Color GetThemedColor(Color? customColor, bool isDark)
    {
        // Si une couleur custom est définie, on la prend
        if (customColor != null)
            return customColor;

        // Sinon on cherche dans les ressources
        var resourceKey = isDark ? "OffBlack" : "White";
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var res) is true && res is Color resColor)
        {
            return resColor;
        }

        // Sinon fallback hardcodé
        return isDark ? DarkFallback : LightFallback;
    }

    private static StatusBarStyle GetStatusBarStyle(Color color)
    {
        // Si la couleur est claire (> 0.5), on veut des icones sombres (DarkContent)
        // Sinon on veut des icones claires (LightContent)
        return color.GetLuminosity() > 0.5 ? StatusBarStyle.DarkContent : StatusBarStyle.LightContent;
    }

#if ANDROID
    private static NavigationBarStyle GetNavigationBarStyle(Color color)
    {
        return color.GetLuminosity() > 0.5 ? NavigationBarStyle.DarkContent : NavigationBarStyle.LightContent;
    }
#endif
}
