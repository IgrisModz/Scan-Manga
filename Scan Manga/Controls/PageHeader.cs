using Microsoft.Maui.Controls.Shapes;

namespace Scan_Manga.Controls;

public partial class PageHeader : Grid
{
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
        layout.Children.Add(image);
        layout.Children.Add(label);

        border.Content = layout;

        Children.Add(border);
    }
}
