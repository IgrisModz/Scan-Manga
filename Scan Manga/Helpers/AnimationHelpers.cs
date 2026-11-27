namespace Scan_Manga.Helpers;

public static class AnimationHelpers
{
	public static Task AnimateWidthAsync(this VisualElement view, double from, double to, uint length = 250, CancellationToken cancellationToken = default) =>
		AnimateAsync(view, new Animation(value => view.WidthRequest = value, from, to, Easing.CubicInOut), length, cancellationToken);

	public static Task AnimateHeightAsync(this VisualElement view, double from, double to, uint length = 250, CancellationToken cancellationToken = default) =>
		AnimateAsync(view, new Animation(value => view.HeightRequest = value, from, to, Easing.CubicInOut), length, cancellationToken);

	public static Task RotateToSafe(this VisualElement view, double rotation, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		AnimateAsync(view, new Animation(value => view.Rotation = value, view.Rotation, rotation, easing ?? Easing.CubicInOut), length, cancellationToken);

	public static Task FadeToSafe(this VisualElement view, double opacity, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		AnimateAsync(view, new Animation(value => view.Opacity = value, view.Opacity, opacity, easing ?? Easing.CubicInOut), length, cancellationToken);

	public static Task ScaleToSafe(this VisualElement view, double scale, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		AnimateAsync(view, new Animation(value => view.Scale = value, view.Scale, scale, easing ?? Easing.CubicInOut), length, cancellationToken);

	public static Task TranslateToSafe(this VisualElement view, double x, double y, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default)
	{
		var animation = new Animation
		{
			{ 0, 1, new Animation(value => view.TranslationX = value, view.TranslationX, x, easing ?? Easing.CubicInOut) },
			{ 0, 1, new Animation(value => view.TranslationY = value, view.TranslationY, y, easing ?? Easing.CubicInOut) }
		};
		return AnimateAsync(view, animation, length, cancellationToken);
	}

	static async Task AnimateAsync(VisualElement view, Animation animation, uint length, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var name = $"ScanMangaAnimation-{Guid.NewGuid():N}";
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		animation.Commit(view, name, 16, length, finished: (_, cancelled) =>
		{
			if (cancelled)
			{
				completion.TrySetCanceled();
			}
			else
			{
				completion.TrySetResult();
			}
		});

		using var registration = cancellationToken.Register(() => MainThread.BeginInvokeOnMainThread(() =>
		{
			view.AbortAnimation(name);
			completion.TrySetCanceled(cancellationToken);
		}));

		await completion.Task.ConfigureAwait(true);
	}
}
