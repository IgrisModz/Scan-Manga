namespace Scan_Manga.Controls;

public class CustomWebView : WebView
{
    public static readonly BindableProperty ProgressProperty =
    BindableProperty.Create(nameof(Progress), typeof(double), typeof(CustomWebView), 0.0, propertyChanged: OnProgressChanged);

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CustomWebView), false);

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CustomWebView webView)
        {
            double p = (double)newValue;

            webView.IsLoading = p < 1.0;
        }
    }
}
