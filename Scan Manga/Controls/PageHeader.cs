using MauiIcons.MaterialSymbols.Rounded;
using Microsoft.Maui.Controls.Shapes;
using Scan_Manga.Behaviors;

namespace Scan_Manga.Controls;

public partial class PageHeader : Grid
{
    readonly Grid backButtonContainer;

    public event EventHandler? BackClicked;

    public bool ShowBackButton
    {
        get => backButtonContainer.IsVisible;
        set => backButtonContainer.IsVisible = value;
    }

    public PageHeader()
    {
        // Resources (équivalent StaticResource)
        var white = Application.Current?.Resources["White"] as Color;
        var offBlack = Application.Current?.Resources["OffBlack"] as Color;
        var primary = Application.Current?.Resources["Primary"] as Color;
        var headlineStyle = Application.Current?.Resources["Headline"] as Style;

        // Border
        var border = new Border
        {
            Margin = 0,
            Padding = new Thickness(20, 0),
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new Rectangle()
        };

        // AppThemeBinding équivalent
        border.SetAppThemeColor(BackgroundProperty, white, offBlack);

        // Shadow
        border.Shadow = new Shadow
        {
            Brush = primary,
            Opacity = 0.8f,
            Radius = 14,
            Offset = new Point(0, 7)
        };

        // Layout horizontal
        var layout = new HorizontalStackLayout();

        backButtonContainer = new Grid
        {
            WidthRequest = 48,
            HeightRequest = 48,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        var backButton = new Button
        {
            Padding = 0,
            BackgroundColor = Colors.Transparent,
            CornerRadius = 24
        };
        var states = new VisualStateGroup { Name = "CommonStates" };
        states.States.Add(new VisualState { Name = "Normal" });
        states.States.Add(new VisualState
        {
            Name = "PointerOver",
            Setters = { new Setter { Property = BackgroundColorProperty, Value = Color.FromArgb("#1A808080") } }
        });
        states.States.Add(new VisualState
        {
            Name = "Pressed",
            Setters = { new Setter { Property = BackgroundColorProperty, Value = Color.FromArgb("#40808080") } }
        });
        states.States.Add(new VisualState
        {
            Name = "Focused",
            Setters = { new Setter { Property = BackgroundColorProperty, Value = Color.FromArgb("#26808080") } }
        });
        states.States.Add(new VisualState
        {
            Name = "Disabled",
            Setters = { new Setter { Property = BackgroundColorProperty, Value = Colors.Transparent } }
        });
		VisualStateManager.SetVisualStateGroups(backButton, [states]);
        SemanticProperties.SetDescription(backButton, "Retour");
        backButton.Clicked += (sender, args) => BackClicked?.Invoke(sender, args);
		CursorBehavior.SetCursor(backButton, CursorIcon.Hand);

        var backIcon = new MaterialSymbolsRoundedIcon
        {
            Icon = MaterialSymbolsRoundedIcons.ArrowBack,
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };
        backIcon.SetAppThemeColor(Label.TextColorProperty, primary, Application.Current?.Resources["PrimaryDark"] as Color);
        backButton.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsEnabled))
            {
                backIcon.Opacity = backButton.IsEnabled ? 1 : 0.4;
            }
        };
        backButtonContainer.Children.Add(backButton);
        backButtonContainer.Children.Add(backIcon);

        //Image
        var image = new Image
        {
            Margin = new Thickness(10, 6),
            HeightRequest = 50,
            WidthRequest = 50,
            Source = "scan_manga_icon.png"
        };
        SemanticProperties.SetDescription(image, "Scan-Manga Icon");

        // Label
        var label = new Label
        {
            Text = "Scan-Manga",
            FontSize = 24,
            VerticalOptions = LayoutOptions.Center,
            Style = headlineStyle
        };

        // Ajout des enfants
        layout.Children.Add(backButtonContainer);
        layout.Children.Add(image);
        layout.Children.Add(label);

        border.Content = layout;

        Children.Add(border);
    }
}
