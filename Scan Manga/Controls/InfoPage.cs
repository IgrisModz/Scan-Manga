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

			var customOverlay = new CustomOverlay();
			Grid.SetRowSpan(customOverlay, 2);
			customOverlay.SetBinding(CustomOverlay.TitleProperty, new Binding("BindingContext.Overlay.Title", source: RelativeBindingSource.TemplatedParent));
			customOverlay.SetBinding(CustomOverlay.MessageProperty, new Binding("BindingContext.Overlay.Message", source: RelativeBindingSource.TemplatedParent));
			customOverlay.SetBinding(CustomOverlay.ConfirmTextProperty, new Binding("BindingContext.Overlay.ConfirmText", source: RelativeBindingSource.TemplatedParent));
			customOverlay.SetBinding(CustomOverlay.CancelTextProperty, new Binding("BindingContext.Overlay.CancelText", source: RelativeBindingSource.TemplatedParent));
			customOverlay.SetBinding(CustomOverlay.IsOpenProperty, new Binding("BindingContext.Overlay.IsVisible", mode: BindingMode.TwoWay, source: RelativeBindingSource.TemplatedParent));
			customOverlay.SetBinding(CustomOverlay.ResponseCommandProperty, new Binding("BindingContext.OverlayResultCommand", source: RelativeBindingSource.TemplatedParent));

			rootGrid.Children.Add(customOverlay);

			return rootGrid;
		});
	}
}
