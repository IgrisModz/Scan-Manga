using MauiIcons.Core.Extensions;
using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Helpers;
using Scan_Manga.Pages;

namespace Scan_Manga.Controls;

public partial class ExpandableNavBar : Grid, IDisposable
{
	const double minWidth = 50;
	const double minHeight = 50;
	const double maxHeight = 110;
	const double extraOptionsOffset = 8;
	const double extraOptionsSpacing = 8;

	const uint heightAnimationDuration = 300;
	const uint widthAnimationDuration = 200;
	const uint rotationAnimationDuration = 500;
	const uint contentAnimationDuration = 120;
	const uint extraOptionsAnimationDuration = 180;

	bool navBarExpanded;
	bool isVerticalExpanded;
	bool disposedValue;
	bool isTransitioning;

	CancellationTokenSource? animationCancellation;
	TapGestureRecognizer? expandButtonGesture;

	public event EventHandler? RefreshClicked;
	public event EventHandler? HomeClicked;

	public ExpandableNavBar()
	{
		InitializeComponent();

		expandButtonGesture = new TapGestureRecognizer
		{
			Command = new Command(() => SafeFireAndForget(token => ExecuteSafelyAsync(OnExpandClickedAsync, token)))
		};

		ExpandBtn.GestureRecognizers.Add(expandButtonGesture);
		SizeChanged += OnSizeChanged;
		SafeAreaEdges = SafeAreaEdges.None;
	}

	#region State & Animation Core

	async Task ExecuteSafelyAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		if (isTransitioning || disposedValue)
		{
			return;
		}

		isTransitioning = true;
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		animationCancellation = linkedCts;

		try
		{
			await action(linkedCts.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
		{
		}
		finally
		{
			animationCancellation = null;
			isTransitioning = false;
			if (navBarExpanded && !disposedValue)
			{
				UpdateExpandedSize();
			}
		}
	}

	// Fires an async operation from a non-async event handler while safely observing any exception,
	// avoiding the need for a try/catch block in every "async void" handler.
	static void SafeFireAndForget(Func<CancellationToken, Task> taskFactory, CancellationToken cancellationToken = default)
	{
		_ = SafeFireAndForgetAsync(taskFactory, cancellationToken);
	}

	static async Task SafeFireAndForgetAsync(Func<CancellationToken, Task> taskFactory, CancellationToken cancellationToken)
	{
		try
		{
			await taskFactory(cancellationToken).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError($"Navigation menu error: {ex}");
		}
	}

	async Task OnExpandClickedAsync(CancellationToken cancellationToken)
	{
		if (!navBarExpanded)
		{
			await OpenNavBarAsync(cancellationToken).ConfigureAwait(true);
			return;
		}

		await CloseNavBarInternalAsync(cancellationToken).ConfigureAwait(true);
	}

	void OnSizeChanged(object? sender, EventArgs e)
	{
		if (navBarExpanded && !isTransitioning && !disposedValue)
		{
			UpdateExpandedSize();
		}
	}

	double GetExpandedWidth()
	{
		var screenWidth = Width > 0
			? Width
			: DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

		return Math.Max(minWidth, Math.Min(screenWidth - MenuHost.Margin.HorizontalThickness, 400));
	}

	double GetExpandedHeight(double width)
	{
		var contentWidth = Math.Max(0, width - NavBar.Padding.HorizontalThickness - ExtraOptionsContainer.Margin.HorizontalThickness);
		var contentHeight = ExtraOptionsContent.Measure(contentWidth, double.PositiveInfinity).Height;
		var screenHeight = Height > 0
			? Height
			: DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
		var availableHeight = Math.Max(0, screenHeight - MenuHost.Margin.VerticalThickness - maxHeight - extraOptionsSpacing);
		ExtraOptionsContainer.HeightRequest = Math.Min(contentHeight, availableHeight);
		return maxHeight + extraOptionsSpacing + ExtraOptionsContainer.HeightRequest;
	}

	void UpdateExpandedSize()
	{
		var width = GetExpandedWidth();
		NavBar.WidthRequest = width;
		NavBar.HeightRequest = isVerticalExpanded ? GetExpandedHeight(width) : maxHeight;
	}

	async Task OpenNavBarAsync(CancellationToken cancellationToken)
	{
		var maxWidth = GetExpandedWidth();

		ClickOutsideOverlay.IsVisible = true;
		ExpandBtn.Rotation = 0;

		var rotationTask = ExpandBtn.RotateToSafe(360, rotationAnimationDuration, cancellationToken: cancellationToken);

		await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, maxWidth, widthAnimationDuration, cancellationToken).ConfigureAwait(true);
		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, maxHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);

		await rotationTask.ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		NavBarContent.IsVisible = true;
		MoreBtnContainer.IsVisible = true;
		await Task.WhenAll(
			ExpandBtnContainer.FadeToSafe(0, contentAnimationDuration, cancellationToken: cancellationToken),
			NavBarContent.FadeToSafe(1, contentAnimationDuration, cancellationToken: cancellationToken),
			MoreBtnContainer.FadeToSafe(1, contentAnimationDuration, cancellationToken: cancellationToken)
		).ConfigureAwait(true);
		ExpandBtnContainer.IsVisible = false;

		navBarExpanded = true;
	}

	async Task CloseNavBarInternalAsync(CancellationToken cancellationToken)
	{
		if (isVerticalExpanded)
		{
			await CloseVerticalMenuAsync(cancellationToken).ConfigureAwait(true);
		}

		ExpandBtnContainer.IsVisible = true;
		await Task.WhenAll(
			ExpandBtnContainer.FadeToSafe(1, contentAnimationDuration, cancellationToken: cancellationToken),
			NavBarContent.FadeToSafe(0, contentAnimationDuration, cancellationToken: cancellationToken),
			MoreBtnContainer.FadeToSafe(0, contentAnimationDuration, cancellationToken: cancellationToken)
		).ConfigureAwait(true);
		NavBarContent.IsVisible = false;
		MoreBtnContainer.IsVisible = false;

		ExpandBtn.Rotation = 360;
		var rotationTask = ExpandBtn.RotateToSafe(0, rotationAnimationDuration, cancellationToken: cancellationToken);

		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, minHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);
		await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, minWidth, widthAnimationDuration, cancellationToken).ConfigureAwait(true);

		await rotationTask.ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		ClickOutsideOverlay.IsVisible = false;
		navBarExpanded = false;
	}

	#endregion

	#region Extra Options Animations

	void OnMoreTapped(object? sender, TappedEventArgs e)
		=> SafeFireAndForget(outerToken => ExecuteSafelyAsync(async cancellationToken =>
		{
			if (!isVerticalExpanded)
			{
				await ShowExtraOptionsAsync(cancellationToken).ConfigureAwait(true);
				return;
			}

			await CloseVerticalMenuAsync(cancellationToken).ConfigureAwait(true);
		}, outerToken));

	async Task ShowExtraOptionsAsync(CancellationToken cancellationToken)
	{
		MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowDown.GetGlyph();

		ExtraOptionsContainer.IsVisible = true;
		ExtraOptionsContainer.Opacity = 0;
		ExtraOptionsContainer.TranslationY = extraOptionsOffset;

		var expandedHeight = GetExpandedHeight(GetExpandedWidth());
		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, expandedHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);

		await Task.WhenAll(
			ExtraOptionsContainer.FadeToSafe(1, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken),
			ExtraOptionsContainer.TranslateToSafe(0, 0, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken)
		).ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		isVerticalExpanded = true;
	}

	async Task CloseVerticalMenuAsync(CancellationToken cancellationToken)
	{
		if (!isVerticalExpanded)
		{
			return;
		}

		MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph();

		await Task.WhenAll(
			ExtraOptionsContainer.FadeToSafe(0, extraOptionsAnimationDuration, Easing.CubicIn, cancellationToken),
			ExtraOptionsContainer.TranslateToSafe(0, extraOptionsOffset, extraOptionsAnimationDuration, Easing.CubicIn, cancellationToken)
		).ConfigureAwait(true);

		ExtraOptionsContainer.IsVisible = false;
		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, maxHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);
		isVerticalExpanded = false;
	}

	#endregion
	
	#region Navigation & Overlay Handlers

	void OnOverlayTapped(object? sender, TappedEventArgs e)
		=> SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));

	void OnOverlayPan(object? sender, PanUpdatedEventArgs e)
	{
		if (e.StatusType == GestureStatus.Started)
		{
			SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));
		}
	}

	void OnOverlayPinch(object? sender, PinchGestureUpdatedEventArgs e)
	{
		if (e.Status == GestureStatus.Started)
		{
			SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));
		}
	}

	void IgnoreOnOverlayTapped(object? sender, TappedEventArgs e) { }

	async Task CloseNavBarAsync(CancellationToken cancellationToken)
	{
		await CloseNavBarInternalAsync(cancellationToken).ConfigureAwait(true);
	}

	void OnNoticesClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<LegalNoticesPage>(cancellationToken));

	void OnPrivacyClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<PrivacyPolicyPage>(cancellationToken));

	void OnTermsClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<TermsOfUsePage>(cancellationToken));

	void OnAboutClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<AboutPage>(cancellationToken));

	void OnDonateClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<DonatePage>(cancellationToken));

	void OnSettingsClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync<SettingsPage>(cancellationToken));

	void OnRefreshClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => RaiseButtonTapEventAsync(RefreshClicked, cancellationToken));

	void OnHomeClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => RaiseButtonTapEventAsync(HomeClicked, cancellationToken));

	Task NavigateToAsync<TPage>(CancellationToken cancellationToken) where TPage : InfoPage
	{
		return ExecuteSafelyAsync(
			innerToken => ButtonTapAsync(async () =>
			{
				var services = Handler?.MauiContext?.Services
					?? throw new InvalidOperationException("Navigation services are unavailable.");
				var page = services.GetRequiredService<TPage>();
#if WINDOWS
				var window = Window ?? throw new InvalidOperationException("Navigation window is unavailable.");
				page.PrepareModalOpening(window.Height);
#endif
				await Navigation.PushModalAsync(page, true);
#if WINDOWS
				await page.AnimateModalOpeningAsync();
#endif
			}, innerToken),
			cancellationToken);
	}

	Task RaiseButtonTapEventAsync(EventHandler? eventHandler, CancellationToken cancellationToken)
	{
		return ExecuteSafelyAsync(
			innerToken => ButtonTapAsync(() =>
			{
				eventHandler?.Invoke(this, EventArgs.Empty);
				return Task.CompletedTask;
			},
			innerToken),
			cancellationToken);
	}

	async Task ButtonTapAsync(Func<Task> action, CancellationToken cancellationToken)
	{
		await CloseNavBarInternalAsync(cancellationToken).ConfigureAwait(true);

		await Task.Delay(50, cancellationToken).ConfigureAwait(true);

		await action().ConfigureAwait(true);
	}
	
	#endregion

	#region IDisposable

	protected virtual void Dispose(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}

		if (disposing)
		{
			animationCancellation?.Cancel();
			SizeChanged -= OnSizeChanged;

			// Remove gesture recognizers created manually.
			if (expandButtonGesture is not null)
			{
				ExpandBtn.GestureRecognizers.Remove(expandButtonGesture);
				expandButtonGesture = null;
			}
		}

		disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	
	#endregion
}