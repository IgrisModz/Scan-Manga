namespace Scan_Manga.Helpers;

public static class AnimationHelpers
{
    public static Task AnimateWidthAsync(VisualElement view, double from, double to, uint length = 250)
    {
        var tcs = new TaskCompletionSource<bool>();
        var animation = new Animation(v => view.WidthRequest = v, from, to, Easing.CubicInOut);
        animation.Commit(view, "AnimateWidth", 16, length, finished: (v, c) => tcs.SetResult(true));
        return tcs.Task;
    }

    public static Task AnimateHeightAsync(VisualElement view, double from, double to, uint length = 250)
    {
        var tcs = new TaskCompletionSource<bool>();
        var animation = new Animation(v => view.HeightRequest = v, from, to, Easing.CubicInOut);
        animation.Commit(view, "AnimateHeight", 16, length, finished: (v, c) => tcs.SetResult(true));
        return tcs.Task;
    }

    public static Task RotateToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
#if NET10_0_OR_GREATER
        return view.RotateToAsync(to, length, easing);
#else
        return view.RotateTo(to, length, easing);
#endif
    }

    public static Task FadeToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
#if NET10_0_OR_GREATER
        return view.FadeToAsync(to, length, easing);
#else
        return view.FadeTo(to, length, easing);
#endif
    }

    public static Task ScaleToSafe(this VisualElement view, double to, uint length = 250, Easing? easing = null)
    {
        easing ??= Easing.CubicInOut;
#if NET10_0_OR_GREATER
        return view.ScaleToAsync(to, length, easing);
#else
        return view.ScaleTo(to, length, easing);
#endif
    }
}
