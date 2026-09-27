using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Platform;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Scan_Manga.Helpers;

public static partial class CursorExtensions
{
	sealed class CursorState(
		CursorIcon cursor,
		PointerEventHandler pointerEntered,
		PointerEventHandler pointerExited)
	{
		public CursorIcon Cursor { get; } = cursor;

		public PointerEventHandler PointerEntered { get; } = pointerEntered;

		public PointerEventHandler PointerExited { get; } = pointerExited;
	}

	static readonly ConditionalWeakTable<UIElement, CursorState> cursorStates = [];

	static readonly MethodInfo? setProtectedCursorMethod =
		typeof(UIElement).GetProperty(
			"ProtectedCursor",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance)!
		.SetMethod;

	public static void SetCustomCursor(this VisualElement visualElement, CursorIcon cursor, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(visualElement);
		ArgumentNullException.ThrowIfNull(mauiContext);

		var nativeView = visualElement.ToPlatform(mauiContext);

		SetCustomCursor(nativeView, cursor);
	}

	static void SetCustomCursor(
		UIElement nativeView,
		CursorIcon cursor)
	{
		RemoveExistingCursor(nativeView);

		void pointerEntered(object sender, PointerRoutedEventArgs e) =>
			SetCursor(nativeView, cursor);

		void pointerExited(object sender, PointerRoutedEventArgs e) =>
			SetCursor(nativeView, CursorIcon.Arrow);

		nativeView.PointerEntered += pointerEntered;
		nativeView.PointerExited += pointerExited;

		cursorStates.Add(
			nativeView,
			new CursorState(cursor, pointerEntered, pointerExited));
	}

	static void RemoveExistingCursor(UIElement nativeView)
	{
		if (!cursorStates.TryGetValue(nativeView, out var state))
		{
			return;
		}

		nativeView.PointerEntered -= state.PointerEntered;
		nativeView.PointerExited -= state.PointerExited;

		cursorStates.Remove(nativeView);
	}

	static void SetCursor(
		UIElement element,
		CursorIcon cursor)
	{
		if (setProtectedCursorMethod is null)
		{
			return;
		}

		setProtectedCursorMethod.Invoke(
			element,
			[CreateCursor(cursor)]);
	}

	static InputSystemCursor CreateCursor(CursorIcon cursor)
		=> cursor switch
		{

			CursorIcon.Hand =>
				InputSystemCursor.Create(InputSystemCursorShape.Hand),

			CursorIcon.IBeam =>
				InputSystemCursor.Create(InputSystemCursorShape.IBeam),

			CursorIcon.Cross =>
				InputSystemCursor.Create(InputSystemCursorShape.Cross),

			CursorIcon.Wait =>
				InputSystemCursor.Create(InputSystemCursorShape.Wait),

			CursorIcon.NotAllowed =>
				InputSystemCursor.Create(InputSystemCursorShape.UniversalNo),

			CursorIcon.Help =>
				InputSystemCursor.Create(InputSystemCursorShape.Help),

			CursorIcon.SizeAll =>
				InputSystemCursor.Create(InputSystemCursorShape.SizeAll),

			CursorIcon.SizeHorizontal =>
				InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),

			CursorIcon.SizeVertical =>
				InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),

			CursorIcon.SizeDiagonalNWSE =>
				InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),

			CursorIcon.SizeDiagonalNESW =>
				InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),

			_ =>
				InputSystemCursor.Create(InputSystemCursorShape.Arrow)
		};
}
