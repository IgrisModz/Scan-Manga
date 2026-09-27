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
	const double animationOffset = 50;

	const uint heightAnimationDuration = 300;
	const uint widthAnimationDuration = 200;
	const uint rotationAnimationDuration = 500;
	const uint extraOptionsAnimationDuration = 180;
	const uint extraOptionsAnimationDelay = 45;

	bool navBarExpanded;
	bool isVerticalExpanded;
	bool disposedValue;

	CancellationTokenSource? animationCancellation;
	TapGestureRecognizer? expandButtonGesture;

	public event EventHandler? RefreshClicked;
	public event EventHandler? HomeClicked;

	IconButton[] ExtraOptionButtons => [NoticesButton, TermsButton, PrivacyButton, AboutButton];

	public ExpandableNavBar()
	{
		InitializeComponent();

		expandButtonGesture = new TapGestureRecognizer
		{
			Command = new Command(async () => await ExecuteSafelyAsync(OnExpandClickedAsync, CancellationToken.None ).ConfigureAwait(true))
		};

		ExpandBtn.GestureRecognizers.Add(expandButtonGesture);
		SafeAreaEdges = SafeAreaEdges.None;
	}

	#region State & Animation Core

	CancellationToken CreateAnimationCancellationToken()
	{
		animationCancellation?.Cancel();
		animationCancellation?.Dispose();
		animationCancellation = new CancellationTokenSource();

		return animationCancellation.Token;
	}



	async Task ExecuteSafelyAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			CreateAnimationCancellationToken(),
			cancellationToken);

		try
		{
			await action(linkedCts.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			// Animation or execution cancelled intentionally
		}
		catch (Exception)
		{
			// TODO: Log the exception.
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
		catch (OperationCanceledException)
		{
			// Execution cancelled intentionally
		}
		catch (Exception)
		{
			// TODO: Log exception
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

	async Task OpenNavBarAsync(CancellationToken cancellationToken)
	{
		var screenWidth = Width > 0
			? Width
			: DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

		var maxWidth = Math.Min(screenWidth - 30, 400);

		ClickOutsideOverlay.IsVisible = true;
		ExpandBtn.Rotation = 0;

		var rotationTask = ExpandBtn.RotateToSafe(360, rotationAnimationDuration, cancellationToken: cancellationToken);

		await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, maxWidth, widthAnimationDuration, cancellationToken).ConfigureAwait(true);
		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, maxHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);

		await rotationTask.ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		ExpandBtn.Text = MaterialSymbolsRoundedIcons.Close.GetGlyph();
		ExpandBtnContainer.IsVisible = false;
		NavBarContent.IsVisible = true;
		MoreBtnContainer.IsVisible = true;

		navBarExpanded = true;
	}

	async Task CloseNavBarInternalAsync(CancellationToken cancellationToken)
	{
		ClickOutsideOverlay.IsVisible = false;

		if (isVerticalExpanded)
		{
			await CloseVerticalMenuAsync(cancellationToken).ConfigureAwait(true);
		}

		NavBarContent.IsVisible = false;
		MoreBtnContainer.IsVisible = false;
		ExpandBtnContainer.IsVisible = true;

		ExpandBtn.Rotation = 360;

		var rotationTask = ExpandBtn.RotateToSafe(0, rotationAnimationDuration, cancellationToken: cancellationToken);

		await AnimationHelpers.AnimateHeightAsync(NavBar, NavBar.Height, minHeight, heightAnimationDuration, cancellationToken).ConfigureAwait(true);
		await AnimationHelpers.AnimateWidthAsync(NavBar, NavBar.Width, minWidth, widthAnimationDuration, cancellationToken).ConfigureAwait(true);

		await rotationTask.ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		ExpandBtn.Text = MaterialSymbolsRoundedIcons.Notes.GetGlyph();
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
		ExtraOptionsContainer.TranslationY = 20;

		var buttons = ExtraOptionButtons;

		foreach (var button in buttons)
		{
			button.Opacity = 0;
			button.TranslationX = animationOffset;
		}

		var containerTask = Task.WhenAll(
			ExtraOptionsContainer.FadeToSafe(1, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken),
			ExtraOptionsContainer.TranslateToSafe(0, 0, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken)
		);

		var buttonsTask = AnimateButtonsInAsync(buttons, cancellationToken);

		await Task.WhenAll(containerTask, buttonsTask).ConfigureAwait(true);
		cancellationToken.ThrowIfCancellationRequested();

		isVerticalExpanded = true;
	}

	static async Task AnimateButtonsInAsync(IReadOnlyList<IconButton> buttons, CancellationToken cancellationToken)
	{
		// Reverse the visual order so the buttons appear from right to left.
		var animations = buttons
			.Reverse()
			.Select((button, index) =>
				AnimateButtonInAsync(button, index, cancellationToken))
			.ToArray();

		await Task.WhenAll(animations).ConfigureAwait(true);
	}

	static async Task AnimateButtonInAsync(IconButton button, int index, CancellationToken cancellationToken)
	{
		await Task.Delay(index * (int)extraOptionsAnimationDelay, cancellationToken).ConfigureAwait(true);

		await Task.WhenAll(
			button.TranslateToSafe(0, 0, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken),
			button.FadeToSafe(1, extraOptionsAnimationDuration, Easing.CubicOut, cancellationToken)
		).ConfigureAwait(true);
	}

	async Task CloseVerticalMenuAsync(CancellationToken cancellationToken)
	{
		if (!isVerticalExpanded)
		{
			return;
		}

		MoreIcon.Text = MaterialSymbolsRoundedIcons.KeyboardArrowUp.GetGlyph();

		var buttons = ExtraOptionButtons;

		var animations = buttons
			.Select((button, index) => AnimateButtonOutAsync(button, index, cancellationToken))
			.ToArray();

		var containerTask = Task.WhenAll(
			ExtraOptionsContainer.FadeToSafe(0, 150, Easing.CubicIn, cancellationToken),
			ExtraOptionsContainer.TranslateToSafe(20, 0, 150, Easing.CubicIn, cancellationToken)
		);

		await Task.WhenAll(containerTask, Task.WhenAll(animations)).ConfigureAwait(true);

		ExtraOptionsContainer.IsVisible = false;
		isVerticalExpanded = false;
	}

	static async Task AnimateButtonOutAsync(IconButton button, int index, CancellationToken cancellationToken)
	{
		await Task.Delay(index * (int)extraOptionsAnimationDelay, cancellationToken).ConfigureAwait(true);

		await Task.WhenAll(
			button.TranslateToSafe(-animationOffset, 0, 150, Easing.CubicIn, cancellationToken),
			button.FadeToSafe(0, 150, Easing.CubicIn, cancellationToken)
		).ConfigureAwait(true);
	}

	#endregion
	
	#region Navigation & Overlay Handlers

	void OnOverlayTapped(object? sender, TappedEventArgs e)
		=> SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));

	void OnOverlayPan(object? sender, PanUpdatedEventArgs e)
		=> SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));

	void OnOverlayPinch(object? sender, PinchGestureUpdatedEventArgs e)
		=> SafeFireAndForget(cancellationToken => ExecuteSafelyAsync(CloseNavBarAsync, cancellationToken));

	void IgnoreOnOverlayTapped(object? sender, TappedEventArgs e) { }

	async Task CloseNavBarAsync(CancellationToken cancellationToken)
	{
		await CloseNavBarInternalAsync(cancellationToken).ConfigureAwait(true);
	}

	void OnNoticesClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(LegalNoticesPage), cancellationToken));

	void OnPrivacyClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(PrivacyPolicyPage), cancellationToken));

	void OnTermsClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(TermsOfUsePage), cancellationToken));

	void OnAboutClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(AboutPage), cancellationToken));

	void OnDonateClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(DonatePage), cancellationToken));

	void OnSettingsClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => NavigateToAsync(nameof(SettingsPage), cancellationToken));

	void OnRefreshClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => RaiseButtonTapEventAsync(RefreshClicked, cancellationToken));

	void OnHomeClicked(object? sender, EventArgs e)
		=> SafeFireAndForget(cancellationToken => RaiseButtonTapEventAsync(HomeClicked, cancellationToken));

	Task NavigateToAsync(string route, CancellationToken cancellationToken)
	{
		return ExecuteSafelyAsync(
			innerToken => ButtonTapAsync(() => Shell.Current.GoToAsync(route), innerToken),
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
			// Cancel and dispose any running animation.
			animationCancellation?.Cancel();
			animationCancellation?.Dispose();
			animationCancellation = null;

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