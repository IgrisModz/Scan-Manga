namespace Scan_Manga.Helpers;

public static class AnimationHelpers
{
    public static Task<bool> AnimateWidthAsync(this VisualElement view, double from, double to, uint length = 250)
        => AnimatePropertyAsync(view, "AnimateWidth", v => view.WidthRequest = v, from, to, length);

    public static Task<bool> AnimateHeightAsync(this VisualElement view, double from, double to, uint length = 250)
        => AnimatePropertyAsync(view, "AnimateHeight", v => view.HeightRequest = v, from, to, length);

    static Task<bool> AnimatePropertyAsync(VisualElement view, string name, Action<double> callback, double from, double to, uint length)
    {
        var tcs = new TaskCompletionSource<bool>();
        var animation = new Animation(callback, from, to, Easing.CubicInOut);
        animation.Commit(view, name, 16, length, finished: (v, c) => tcs.SetResult(true));
        return tcs.Task;
    }

    public static Task RotateToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
        return view.RotateToAsync(to, length, easing);
    }

    public static Task FadeToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
        return view.FadeToAsync(to, length, easing);
    }

    public static Task ScaleToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
        return view.ScaleToAsync(to, length, easing);
    }
}
