namespace Scan_Manga.Controls;

public class CustomWebView : WebView
{
    public const string DefaultUrl = "https://m.scan-manga.com/?po";

    public event EventHandler<WebViewErrorEventArgs>? HttpErrorOccurred;

    public static readonly BindableProperty ProgressProperty =
    BindableProperty.Create(nameof(Progress), typeof(double), typeof(CustomWebView), 0.0, propertyChanged: OnProgressChanged);

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CustomWebView), false);


    public static readonly BindableProperty HasErrorProperty =
        BindableProperty.Create(nameof(HasError), typeof(bool), typeof(CustomWebView), false);

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

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        private set => SetValue(HasErrorProperty, value);
    }

    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }

    private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CustomWebView webView)
        {
            double p = (double)newValue;

            webView.IsLoading = p < 1.0;
        }
    }

    public void RaiseError(string title, string message)
    {
        HasError = true;
        LastErrorCode = title;
        LastErrorMessage = message;
        HttpErrorOccurred?.Invoke(this, new WebViewErrorEventArgs(title, message));
    }

    public void ReloadPage()
    {
        HasError = false;
        Reload();
    }
}

// Classe d’arguments pour l’événement
public class WebViewErrorEventArgs(string title, string message) : EventArgs
{
    public string Title { get; } = title;
    public string Message { get; } = message;
}
