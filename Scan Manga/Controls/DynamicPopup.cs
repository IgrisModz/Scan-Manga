using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Scan_Manga.Services;

namespace Scan_Manga.Controls;

public class DynamicPopup<T> : ContentView
{
    private TaskCompletionSource<PopupResult<T>>? _tcs;
    private readonly Label _titleLabel;
    private readonly VerticalStackLayout _contentStack;
    private readonly Border _popupBorder;

    #region Bindable Properties

    // Title
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(DynamicPopup<T>),
            string.Empty,
            propertyChanged: OnTitleChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DynamicPopup<T> popup && newValue is string title)
        {
            popup._titleLabel.Text = title;
        }
    }

    // Buttons / Content
    public static readonly BindableProperty ButtonsProperty =
        BindableProperty.Create(
            nameof(Buttons),
            typeof(Dictionary<string, T>),
            typeof(DynamicPopup<T>),
            new Dictionary<string, T>(),
            propertyChanged: OnButtonsChanged);

    public Dictionary<string, T> Buttons
    {
        get => (Dictionary<string, T>)GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    private static void OnButtonsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DynamicPopup<T> popup && newValue is Dictionary<string, T> buttons)
        {
            popup._contentStack.Children.Clear();
            foreach (var kv in buttons)
            {
                var btn = new Button { Text = kv.Key, Scale = 0.8f, Opacity = 0f };
                btn.Clicked += async (_, __) => await popup.CloseAsync(kv.Value);
                popup._contentStack.Children.Add(btn);
            }
        }
    }

    #endregion

    public DynamicPopup()
    {
        var overlay = new Grid
        {
            BackgroundColor = Colors.Black.MultiplyAlpha(0.251f),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };

        void OnClose(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(1); // éviter la fermeture pendant le geste
                await CloseAsync(default!, true);
            });
        }

        var systemBars = ServiceHelper.Services.GetRequiredService<ISystemBarsService>();

        if (systemBars == null || systemBars.CurrentMode == SystemBarsMode.Default)
        {
            var overlayPanGesture = new PanGestureRecognizer();
            overlayPanGesture.PanUpdated += (_, _) => OnClose(null, EventArgs.Empty);
            overlay.GestureRecognizers.Add(overlayPanGesture);

            var overlayPinchGesture = new PinchGestureRecognizer();
            overlayPinchGesture.PinchUpdated += (_, _) => OnClose(null, EventArgs.Empty);
            overlay.GestureRecognizers.Add(overlayPinchGesture);
        }

        overlay.GestureRecognizers.Add(new TapGestureRecognizer() { Command = new Command(() => OnClose(null, EventArgs.Empty))});

        _popupBorder = new Border
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(20),
            Shadow = new Shadow
            {
                Offset = new Point(0, 2),
                Opacity = 0.8f,
                Radius = 8
            },
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 }
        };

        const double horizontalMargin = 20; // ta marge souhaitée

        var maxWidth = GetAndroidMaxWidth(horizontalMargin);
        if (!double.IsNaN(maxWidth))
        {
            _popupBorder.MaximumWidthRequest = maxWidth;
        }

        var popupBorderTapGesture = new TapGestureRecognizer();
        popupBorderTapGesture.Tapped += (_, _) => { };
        _popupBorder.GestureRecognizers.Add(popupBorderTapGesture);

        // Titre dynamique
        _titleLabel = new Label
        {
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Black
        };

        // Contenu dynamique (boutons)
        _contentStack = new VerticalStackLayout
        {
            Spacing = 10
        };

        // Contenu du Border
        _popupBorder.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { _titleLabel, _contentStack }
        };

        overlay.Children.Add(_popupBorder);
        Content = overlay;

        // IsVisible = false;

        // Support thème dynamique
        this.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#80000000"), Color.FromArgb("#80000000"));
        _popupBorder.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E0FFFFFF"), Color.FromArgb("#EE000000"));
        _titleLabel.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        _popupBorder.Shadow.SetAppThemeColor(Shadow.BrushProperty, Colors.Black, Colors.White);
    }

    /// <summary>
    /// Affiche le popup et renvoie un Task qui se complète lorsque le popup est fermé.
    /// </summary>
    public async Task<PopupResult<T>> ShowAsync(Page page)
    {
        ArgumentNullException.ThrowIfNull(page, nameof(page));

        _tcs = new TaskCompletionSource<PopupResult<T>>();

        if (page is not ContentPage contentPage)
            throw new InvalidOperationException("Le popup ne peut être affiché que sur une ContentPage.");

        Layout rootLayout;
        if (contentPage.Content is Layout layout)
        {
            rootLayout = layout;
        }
        else
        {
            // Wrap le content existant dans un AbsoluteLayout
            var absolute = new AbsoluteLayout();
            if (contentPage.Content != null)
            {
                AbsoluteLayout.SetLayoutBounds(contentPage.Content, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(contentPage.Content, AbsoluteLayoutFlags.All);
                absolute.Children.Add(contentPage.Content);
            }
            contentPage.Content = absolute;
            rootLayout = absolute;
        }

        if (Parent == null)
        {
            AbsoluteLayout.SetLayoutBounds(this, new Rect(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.All);
            rootLayout.Children.Add(this);
        }


        // IsVisible = true;
        _popupBorder.Opacity = 0;
        _popupBorder.Scale = 0.8;

        // Animation popup
        await Task.Yield();
        await Task.WhenAll(
            _popupBorder.FadeTo(1, 250, Easing.CubicInOut),
            _popupBorder.ScaleTo(1, 250, Easing.CubicOut)
        );

        // Animation des boutons avec léger chevauchement
        foreach (var btn in _contentStack.Children.OfType<Button>())
        {
            AnimateButtonEntry(btn);
        }

        return await _tcs.Task;
    }

    /// <summary>
    /// Ferme le popup et renvoie la valeur.
    /// </summary>
    public async Task CloseAsync(T result, bool dismissedByOutside = false)
    {
        try
        {
            if (!IsVisible) return;

            // Faire disparaître boutons en parallèle
            foreach (var btn in _contentStack.Children.OfType<Button>())
            {
                _ = btn.FadeTo(0, 150);
            }

            // Popup disparition
            await Task.WhenAll(
                _popupBorder.FadeTo(0, 250),
                _popupBorder.ScaleTo(0.8, 250)
            );

            // IsVisible = false;

            // Retirer du parent si possible
            if (Parent is Layout parentLayout)
            {
                parentLayout.Children.Remove(this);
            }

            // Compléter le TaskCompletionSource
            _tcs?.TrySetResult(new PopupResult<T>(result, dismissedByOutside));
            _tcs = null;
        }
        catch
        {
            // Même en cas d'erreur, s'assurer que le Task est complété
            _tcs?.TrySetResult(new PopupResult<T>(result, dismissedByOutside));
            _tcs = null;
        }
    }

    /// <summary>
    /// Définir le contenu dynamiquement en code (optionnel si BindableProperty est utilisé)
    /// </summary>
    public void SetContent(string title, Dictionary<string, T> buttons)
    {
        Title = title;
        Buttons = buttons;
    }

    private static async void AnimateButtonEntry(Button btn)
    {
        await Task.Delay(Random.Shared.Next(50, 150)); // léger décalage aléatoire pour effet naturel
        await Task.WhenAll(
            btn.FadeTo(1, 300, Easing.CubicInOut),
            btn.ScaleTo(1, 300, Easing.CubicOut)
        );
    }

    private double GetAndroidMaxWidth(double horizontalMargin)
    {
        if (DeviceInfo.Platform != DevicePlatform.Android)
            return double.NaN; // laisse MAUI gérer automatiquement pour les autres plateformes

        var display = DeviceDisplay.MainDisplayInfo;
        var screenWidthDip = display.Width / display.Density;

        return screenWidthDip - (horizontalMargin * 2);
    }
}
