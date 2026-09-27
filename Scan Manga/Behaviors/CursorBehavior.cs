using Scan_Manga.Helpers;

namespace Scan_Manga.Behaviors;

public class CursorBehavior
{
	public static readonly BindableProperty CursorProperty =
		BindableProperty.CreateAttached(
			"Cursor",
			typeof(CursorIcon),
			typeof(CursorBehavior),
			CursorIcon.Arrow,
			propertyChanged: OnCursorChanged);

	public static CursorIcon GetCursor(BindableObject bindable) =>
		(CursorIcon)bindable.GetValue(CursorProperty);

	public static void SetCursor(BindableObject bindable, CursorIcon value) =>
		bindable.SetValue(CursorProperty, value);

	static void OnCursorChanged(
		BindableObject bindable,
		object oldvalue,
		object newvalue)
	{
		if (bindable is not VisualElement visualElement)
		{
			return;
		}

		if (visualElement.Handler is not null)
		{
			ApplyCursor(visualElement);
			return;
		}
		
		visualElement.HandlerChanged += OnHandlerChanged;
	}

	static void OnHandlerChanged(object? sender, EventArgs e)
	{
		if (sender is not VisualElement visualElement)
		{
			return;
		}

		visualElement.HandlerChanged -= OnHandlerChanged;

		if (visualElement.Handler is null)
		{
			return;
		}

		ApplyCursor(visualElement);
	}

	static void ApplyCursor(VisualElement visualElement)
	{
		var mauiContext = visualElement.Handler?.MauiContext;

		if (mauiContext is null)
		{
			return;
		}

		visualElement.SetCustomCursor(
			GetCursor(visualElement),
			mauiContext);
	}
}
