using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Helpers;

namespace Scan_Manga.Controls;

public partial class IconButton : ContentView
{
    public event EventHandler? Clicked;

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(MaterialSymbolsRoundedIcons), typeof(IconButton));

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(IconButton));

    public static readonly BindableProperty RotateIconOnClickProperty =
        BindableProperty.Create(nameof(RotateIconOnClick), typeof(bool), typeof(IconButton));

    public IconButton()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public MaterialSymbolsRoundedIcons Icon
    {
        get => (MaterialSymbolsRoundedIcons)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool RotateIconOnClick
    {
        get => (bool)GetValue(RotateIconOnClickProperty);
        set => SetValue(RotateIconOnClickProperty, value);
    }

    async void OnButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VerticalStackLayout tappedBtn)
        {
            Task<bool>? rotationTask = null;

            if (RotateIconOnClick)
            {
                var btnLabel = tappedBtn.Children.OfType<Label>().FirstOrDefault();
                if (btnLabel != null)
                {
                    btnLabel.Rotation = 0;
                    rotationTask = btnLabel.RotateToAsync(360, 500);
                }
            }

            await tappedBtn.ScaleToAsync(.7, 100);
            await tappedBtn.ScaleToAsync(1, 100);

            if (rotationTask is not null)
            {
                await rotationTask;
            }

            Clicked?.Invoke(this, e);
        }
    }
}