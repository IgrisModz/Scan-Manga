namespace Scan_Manga.Controls;

public partial class InfoPage : PageBase
{
	public InfoPage()
	{
		ControlTemplate = new ControlTemplate(() =>
		{
			var header = new PageHeader();

			var contentPresenter = new ContentPresenter();

			var border = new Border
			{
				Margin = new Thickness(20),
				Padding = new Thickness(20),
				VerticalOptions = LayoutOptions.Start,
				Content = contentPresenter
			};

			border.SetDynamicResource(ShadowProperty, "BubbleShadow");

			var contentGrid = new Grid
			{
				MaximumWidthRequest = 800
			};
			contentGrid.Children.Add(border);

			var scrollView = new ScrollView
			{
				Content = contentGrid
			};

			Grid.SetRow(scrollView, 1);

			var rootGrid = new Grid
			{
				RowDefinitions =
				[
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				]
			};

			rootGrid.Children.Add(header);
			rootGrid.Children.Add(scrollView);

			return rootGrid;
		});
	}
}
