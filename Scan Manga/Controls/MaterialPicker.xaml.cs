using CommunityToolkit.Maui.Extensions;
using Scan_Manga.Models;

namespace Scan_Manga.Controls;

public enum PickerVariant { Outlined, Filled, Text }

public partial class MaterialPicker : ContentView
{
    public static readonly BindableProperty SelectedOptionProperty =
        BindableProperty.Create(nameof(SelectedOption), typeof(SelectOption), typeof(MaterialPicker), null, BindingMode.TwoWay);

    public static readonly BindableProperty OptionsProperty =
        BindableProperty.Create(nameof(Options), typeof(IList<SelectOption>), typeof(MaterialPicker), new List<SelectOption>());

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(PickerVariant), typeof(MaterialPicker), PickerVariant.Outlined,
            propertyChanged: (b, o, n) => ((MaterialPicker)b).UpdateStyle((PickerVariant)n));

    public SelectOption SelectedOption
    {
        get => (SelectOption)GetValue(SelectedOptionProperty);
        set => SetValue(SelectedOptionProperty, value);
    }

    public IList<SelectOption> Options
    {
        get => (IList<SelectOption>)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public PickerVariant Variant
    {
        get => (PickerVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MaterialPicker()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (SelectedOption == null && Options.Count > 0)
                SelectedOption = Options[0];

            UpdateStyle(Variant);
        };
    }

    private void UpdateStyle(PickerVariant variant)
    {
        VisualStateManager.GoToState(MainBorder, variant.ToString());

        if (variant == PickerVariant.Outlined)
        {
            MainBorder.StrokeThickness = 1;
            MainBorder.Stroke = GetResourceColor("Gray400", Colors.Gray);
            IndicatorLine.IsVisible = false;
        }
        else
        {
            // Forcer l'absence de bordure pour Filled et Text
            MainBorder.StrokeThickness = 0;
            MainBorder.Stroke = Colors.Transparent;

            IndicatorLine.IsVisible = true;
            IndicatorLine.BackgroundColor = GetResourceColor("Gray400", Colors.Gray);
        }
    }

    private async void OnPickerTapped(object sender, TappedEventArgs e)
    {
        SetActiveState(true);

        var popup = new SelectionPopup(Options, SelectedOption);
        var result = await Shell.Current.CurrentPage.ShowPopupAsync<SelectOption>(popup);

        if (result.Result is SelectOption selected)
        {
            SelectedOption = selected;
        }

        SetActiveState(false);
    }

    private void SetActiveState(bool active)
    {
        if (active)
        {
            var lightColor = GetResourceColor("Primary", Color.FromArgb("#6fc2f4"));
            var darkColor = GetResourceColor("PrimaryDark", Color.FromArgb("#3D96FF"));
            if (Variant == PickerVariant.Outlined)
            {
                MainBorder.SetAppThemeColor(Border.StrokeProperty, lightColor, darkColor);
            }
            else
            {
                IndicatorLine.SetAppThemeColor(BackgroundColorProperty, lightColor, darkColor);
            }
        }
        else
        {
            var color = GetResourceColor("Gray400", Colors.Gray);
            if (Variant == PickerVariant.Outlined)
            {
                MainBorder.Stroke = color;
            }
            else
            {
                MainBorder.Stroke = Colors.Transparent;
                IndicatorLine.BackgroundColor = color;
            }
        }
    }

    private static Color GetResourceColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color color)
            return color;
        return fallback;
    }
}