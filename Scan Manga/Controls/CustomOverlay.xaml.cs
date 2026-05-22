using Scan_Manga.Helpers;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Scan_Manga.Controls;

public partial class CustomOverlay : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(CustomOverlay));
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(CustomOverlay));
    public static readonly BindableProperty ConfirmTextProperty = BindableProperty.Create(nameof(ConfirmText), typeof(string), typeof(CustomOverlay), "OK");
    public static readonly BindableProperty CancelTextProperty = BindableProperty.Create(nameof(CancelText), typeof(string), typeof(CustomOverlay), string.Empty);
    public static readonly BindableProperty ResponseCommandProperty = BindableProperty.Create(nameof(ResponseCommand), typeof(ICommand), typeof(CustomOverlay));

    // Utilisation de IsOpen pour piloter l'UI sans bloquer la page au démarrage
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(CustomOverlay), false, propertyChanged: OnIsOpenChanged);

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string ConfirmText { get => (string)GetValue(ConfirmTextProperty); set => SetValue(ConfirmTextProperty, value); }
    public string CancelText { get => (string)GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }
    public ICommand ResponseCommand { get => (ICommand)GetValue(ResponseCommandProperty); set => SetValue(ResponseCommandProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    public bool IsCancelVisible => !string.IsNullOrEmpty(CancelText);

    public CustomOverlay()
    {
        InitializeComponent();
        IsVisible = false;
        InputTransparent = true;
    }

    static async void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CustomOverlay control && newValue is bool open)
        {
            if (open)
            {
                await control.AnimateIn();
            }
            else
            {
                await control.AnimateOut();
            }
        }
    }

    async Task AnimateIn()
    {
        IsVisible = true;
        InputTransparent = false;

        // Reset visuel
        BackgroundOverlay.Opacity = 0;
        Container.Scale = 0.8;
        Container.Opacity = 0;

        await Task.WhenAll(
            BackgroundOverlay.FadeToSafe(1, 250),
            Container.FadeToSafe(1, 200),
            Container.ScaleToSafe(1, 400, Easing.SpringOut)
        );
    }

    async Task AnimateOut()
    {
        await Task.WhenAll(
            BackgroundOverlay.FadeToSafe(0, 200),
            Container.FadeToSafe(0, 200),
            Container.ScaleToSafe(0.8, 200, Easing.CubicIn)
        );
        IsVisible = false;
        InputTransparent = true;
    }

    void OnConfirmClicked(object sender, EventArgs e) => HandleResponse(true);
    void OnCancelClicked(object sender, EventArgs e) => HandleResponse(false);

    void HandleResponse(bool result)
    {
        // On ferme
        IsOpen = false;

        // On envoie le booléen explicitement au ViewModel
        if (ResponseCommand != null && ResponseCommand.CanExecute(result))
        {
            ResponseCommand.Execute(result);
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(CancelText))
        {
            OnPropertyChanged(nameof(IsCancelVisible));
        }
    }

    void OnBackgroundTapped(object sender, TappedEventArgs e)
    {
        IsOpen = false;
    }

    void OnBorderTapped(object sender, TappedEventArgs e)
    {
    }
}