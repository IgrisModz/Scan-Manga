using Scan_Manga.Helpers;

namespace Scan_Manga.Controls;

public partial class InfoPage : PageBase
{
	const uint openingAnimationDuration = 250;
	const double openingOffset = 16;

	Grid? templateRoot;
	CancellationTokenSource? openingAnimation;
	bool isAppearing;
	bool openingStarted;

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
				Opacity = 0,
				TranslationY = openingOffset,
				RowDefinitions =
				[
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				]
			};
			templateRoot = rootGrid;
			rootGrid.Loaded += OnTemplateLoaded;

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

	protected override void OnAppearing()
	{
		base.OnAppearing();
		isAppearing = true;

		if (templateRoot?.IsLoaded == true)
		{
			StartOpeningAnimation(templateRoot);
		}
	}

	protected override void OnDisappearing()
	{
		isAppearing = false;
		openingStarted = false;
		var cancellation = openingAnimation;
		openingAnimation = null;
		cancellation?.Cancel();
		if (templateRoot is not null)
		{
			templateRoot.Opacity = 1;
			templateRoot.TranslationY = 0;
		}
		base.OnDisappearing();
	}

	void OnTemplateLoaded(object? sender, EventArgs e)
	{
		if (isAppearing && sender is Grid root)
		{
			StartOpeningAnimation(root);
		}
	}

	async void StartOpeningAnimation(Grid root)
	{
		if (openingStarted)
		{
			return;
		}

		openingStarted = true;
		using var cancellation = new CancellationTokenSource();
		openingAnimation = cancellation;
		root.Opacity = 0;
		root.TranslationY = openingOffset;

		try
		{
			await Task.WhenAll(
				root.FadeToSafe(1, openingAnimationDuration, Easing.CubicOut, cancellation.Token),
				root.TranslateToSafe(0, 0, openingAnimationDuration, Easing.CubicOut, cancellation.Token)
			).ConfigureAwait(true);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError($"Page opening animation error: {ex}");
		}
		finally
		{
			if (openingAnimation == cancellation)
			{
				root.Opacity = 1;
				root.TranslationY = 0;
				openingAnimation = null;
			}
		}
	}
}
