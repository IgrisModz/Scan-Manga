#if WINDOWS
using Scan_Manga.Helpers;
#endif

namespace Scan_Manga.Controls;

public partial class InfoPage : PageBase
{
	bool isClosing;
#if WINDOWS
	const uint modalAnimationDuration = 350;
	bool isOpening;
#endif

	public InfoPage()
	{
		ControlTemplate = new ControlTemplate(() =>
		{
			var header = new PageHeader { ShowBackButton = true };
			header.BackClicked += OnBackClicked;

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

#if WINDOWS
	internal void PrepareModalOpening(double height)
	{
		isOpening = true;
		TranslationY = Math.Max(height, 1);
	}

	internal async Task AnimateModalOpeningAsync()
	{
		try
		{
			await this.TranslateToSafe(0, 0, modalAnimationDuration, Easing.CubicOut);
		}
		finally
		{
			TranslationY = 0;
			isOpening = false;
		}
	}
#endif

	protected override bool OnBackButtonPressed()
	{
#if WINDOWS
		OnBackClicked(this, EventArgs.Empty);
		return true;
#else
		return isClosing || base.OnBackButtonPressed();
#endif
	}

	protected async void OnBackClicked(object? sender, EventArgs e)
	{
		if (isClosing
#if WINDOWS
			|| isOpening
#endif
			)
		{
			return;
		}

		isClosing = true;
		try
		{
#if WINDOWS
			await this.TranslateToSafe(0, Math.Max(Height, Window?.Height ?? 0), modalAnimationDuration, Easing.CubicIn);
#endif
			await Navigation.PopModalAsync(true);
		}
		catch (Exception ex)
		{
#if WINDOWS
			TranslationY = 0;
#endif
			System.Diagnostics.Trace.TraceError($"Modal navigation error: {ex}");
		}
		finally
		{
			isClosing = false;
		}
	}
}
