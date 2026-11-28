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

#if ANDROID
    bool isAppearing;
#endif

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

	public virtual bool NavigationBarContrastEnforced { get; set; }

	protected override void OnAppearing()
    {
        base.OnAppearing();

        Application.Current?.RequestedThemeChanged += OnThemeChanged;

#if ANDROID
        isAppearing = true;
        Application.Current?.ModalPushed += OnModalPushed;
        Application.Current?.ModalPopped += OnModalPopped;

        Window?.DisableFullScreen();
        UpdateSystemBars();
#else
        UpdateSystemBars();
        Window?.DisableFullScreen();
#endif
    }

    protected override void OnDisappearing()
    {
        Application.Current?.RequestedThemeChanged -= OnThemeChanged;
#if ANDROID
        isAppearing = false;
        Application.Current?.ModalPushed -= OnModalPushed;
        Application.Current?.ModalPopped -= OnModalPopped;
#endif

        base.OnDisappearing();
    }

    void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => UpdateSystemBars();

#if ANDROID
    void OnModalPushed(object? sender, ModalPushedEventArgs e)
    {
        if (e.Modal == this)
        {
            UpdateSystemBars();
        }
    }

    void OnModalPopped(object? sender, ModalPoppedEventArgs e)
    {
        // Dismiss() est asynchrone : attendre que le fragment Android quitte sa fenêtre.
        Dispatcher.Dispatch(() =>
        {
            if (isAppearing && !FullScreenHelper.IsFullscreen)
            {
                UpdateSystemBars();
            }
        });
    }
#endif

    void UpdateSystemBars()
    {
        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

		var finalStatusBarColor = GetThemedColor(CustomStatusBarColor, isDarkMode);
		var finalNavBarColor = GetThemedColor(CustomNavigationBarColor, isDarkMode);

		if (OperatingSystem.IsAndroidVersionAtLeast(23) || OperatingSystem.IsIOSVersionAtLeast(15))
		{
			StatusBar.SetColor(finalStatusBarColor);
			StatusBar.SetStyle(GetStatusBarStyle(finalStatusBarColor));
		}

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
			NavigationBar.SetColor(this, finalNavBarColor);
			NavigationBar.SetStyle(this, GetNavigationBarStyle(finalNavBarColor));

            // Une valeur inchangée ne déclenche pas le mapper sur une nouvelle fenêtre.
            Handler?.UpdateValue(NavigationBar.ColorProperty.PropertyName);
            Handler?.UpdateValue(NavigationBar.StyleProperty.PropertyName);
		}

        // Désactive le scrim de contraste Android (blanc semi-transparent) que le système
        // applique sur la barre de navigation en mode edge-to-edge. Le contraste est déjà
        // assuré par SetStyle (DarkContent/LightContent) ci-dessus.
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            if (Platform.CurrentActivity?.Window is Android.Views.Window nativeWindow)
            {
                nativeWindow.NavigationBarContrastEnforced = NavigationBarContrastEnforced;
            }
        }
#endif
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
