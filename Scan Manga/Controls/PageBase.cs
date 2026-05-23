using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using MauiFullScreen;


#if ANDROID
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
#endif

namespace Scan_Manga.Controls;

public partial class PageBase : ContentPage
{
    // Couleurs de fallback mises en cache pour éviter le parsing hex à chaque appel
    static readonly Color darkFallback = Color.FromArgb("#1F1F1F");
    static readonly Color lightFallback = Colors.White;

    public static readonly BindableProperty CustomStatusBarColorProperty =
        BindableProperty.Create(nameof(CustomStatusBarColor), typeof(Color), typeof(PageBase), null);

    public static readonly BindableProperty CustomNavigationBarColorProperty =
        BindableProperty.Create(nameof(CustomNavigationBarColor), typeof(Color), typeof(PageBase), null);

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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Application.Current?.RequestedThemeChanged += OnThemeChanged;

        UpdateSystemBars();
        Window?.DisableFullScreen();
    }

    protected override void OnDisappearing()
    {
        Application.Current?.RequestedThemeChanged -= OnThemeChanged;

        base.OnDisappearing();
    }

    void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => UpdateSystemBars();

    void UpdateSystemBars()
    {
        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        var finalStatusBarColor = GetThemedColor(CustomStatusBarColor, isDarkMode);
        var finalNavBarColor = GetThemedColor(CustomNavigationBarColor, isDarkMode);

        // Update Navigation bar style and color
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var navPlatform = On<Microsoft.Maui.Controls.PlatformConfiguration.Android>();
            navPlatform.SetColor(finalNavBarColor);
            // Calcul du style spécifiquement pour la barre de navigation
            navPlatform.SetStyle(GetNavigationBarStyle(finalNavBarColor));
        }
#endif

        if (OperatingSystem.IsAndroidVersionAtLeast(23) || OperatingSystem.IsIOSVersionAtLeast(15))
        {
            // Update Status bar style and color
            StatusBar.SetColor(finalStatusBarColor);
            StatusBar.SetStyle(GetStatusBarStyle(finalStatusBarColor));
        }
    }

    static Color GetThemedColor(Color? customColor, bool isDark)
    {
		// Si une couleur custom est définie, on la prend
		if (customColor != null)
		{
			return customColor;
		}

		// Sinon on cherche dans les ressources
		var resourceKey = isDark ? "OffBlack" : "White";
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var res) is true && res is Color resColor)
        {
            return resColor;
        }

        // Sinon fallback hardcodé
        return isDark ? darkFallback : lightFallback;
    }

    static StatusBarStyle GetStatusBarStyle(Color color)
    {
        // Si la couleur est claire (> 0.5), on veut des icones sombres (DarkContent)
        // Sinon on veut des icones claires (LightContent)
        return color.GetLuminosity() > 0.5 ? StatusBarStyle.DarkContent : StatusBarStyle.LightContent;
    }

#if ANDROID
    static NavigationBarStyle GetNavigationBarStyle(Color color)
    {
        return color.GetLuminosity() > 0.5 ? NavigationBarStyle.DarkContent : NavigationBarStyle.LightContent;
    }
#endif
}
