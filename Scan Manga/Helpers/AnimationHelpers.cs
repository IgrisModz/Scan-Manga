namespace Scan_Manga.Helpers;

public static class AnimationHelpers
{
	public static Task<bool> AnimateWidthAsync(this VisualElement view, double from, double to, uint length = 250, CancellationToken cancellationToken = default) =>
		AnimatePropertyAsync(view, "AnimateWidth", value => view.WidthRequest = value, from, to, length, cancellationToken);

	public static Task<bool> AnimateHeightAsync(this VisualElement view, double from, double to, uint length = 250, CancellationToken cancellationToken = default) =>
		AnimatePropertyAsync(view, "AnimateHeight", value => view.HeightRequest = value, from, to, length, cancellationToken);

	static Task<bool> AnimatePropertyAsync(VisualElement view, string name, Action<double> callback, double start, double end, uint length, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		view.AbortAnimation(name);

		var taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		var animation = new Animation(callback, start, end, Easing.CubicInOut);

		using var registration = cancellationToken.Register(() =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				view.AbortAnimation(name);
				taskSource.TrySetCanceled(cancellationToken);
			});
		});

		animation.Commit(view, name, 16, length, finished: (_, cancelled) =>
		{
			if (cancelled)
			{
				taskSource.TrySetCanceled(cancellationToken);
				return;
			}

			taskSource.TrySetResult(true);
		});

		return taskSource.Task;
	}

	public static Task<bool> RotateToSafe(this VisualElement view, double rotation, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		WaitForCancellationAsync(view.RotateToAsync(rotation, length, easing ?? Easing.CubicInOut), cancellationToken);
	
	public static Task<bool> FadeToSafe(this VisualElement view, double opacity, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		WaitForCancellationAsync(view.FadeToAsync(opacity, length, easing ?? Easing.CubicInOut), cancellationToken);
	
	public static Task<bool> ScaleToSafe(this VisualElement view, double scale, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		WaitForCancellationAsync(view.ScaleToAsync(scale, length, easing ?? Easing.CubicInOut), cancellationToken);

	public static Task<bool> TranslateToSafe(this VisualElement view, double x, double y, uint length = 250, Easing? easing = null, CancellationToken cancellationToken = default) =>
		WaitForCancellationAsync(view.TranslateToAsync(x, y, length, easing ?? Easing.CubicInOut), cancellationToken);

	static Task<bool> WaitForCancellationAsync(Task<bool> animationTask, CancellationToken cancellationToken)
	{
		return animationTask.WaitAsync(cancellationToken);
	}
	sealed record AnimationState(VisualElement View, string Name, TaskCompletionSource<bool> TaskSource, CancellationToken CancellationToken);
}