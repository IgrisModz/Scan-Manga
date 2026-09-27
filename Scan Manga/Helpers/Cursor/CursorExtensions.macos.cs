using AppKit;
using Microsoft.Maui.Platform;
using UIKit;

namespace Scan_Manga.Helpers;

public static partial class CursorExtensions
{
	public static void SetCustomCursor(this VisualElement visualElement, CursorIcon cursor, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		var view = visualElement.ToPlatform(mauiContext);
		if (view.GestureRecognizers is not null)
		{
			foreach (var recognizer in view.GestureRecognizers.OfType<PointerUIHoverGestureRecognizer>())
			{
				view.RemoveGestureRecognizer(recognizer);
			}
		}

		view.AddGestureRecognizer(new PointerUIHoverGestureRecognizer(r =>
		{
			switch (r.State)
			{
				case UIGestureRecognizerState.Began:
					GetNSCursor(cursor)?.Set();
					break;
				case UIGestureRecognizerState.Ended:
					NSCursor.ArrowCursor.Set();
					break;
			}
		}));
	}

	static NSCursor? GetNSCursor(CursorIcon cursor)
	{
		return cursor switch
		{
			CursorIcon.Arrow => NSCursor.ArrowCursor,
			CursorIcon.Hand => NSCursor.PointingHandCursor,
			CursorIcon.IBeam => NSCursor.IBeamCursor,
			CursorIcon.Cross => NSCursor.CrosshairCursor,
			CursorIcon.NotAllowed => NSCursor.OperationNotAllowedCursor,
			CursorIcon.Help => NSCursor.ContextualMenuCursor,
			CursorIcon.SizeAll => NSCursor.OpenHandCursor,
			CursorIcon.SizeHorizontal => NSCursor.ResizeLeftRightCursor,
			CursorIcon.SizeVertical => NSCursor.ResizeUpDownCursor,
			CursorIcon.Grab => NSCursor.OpenHandCursor,
			CursorIcon.Grabbing => NSCursor.ClosedHandCursor,
			CursorIcon.ZoomIn =>
				OperatingSystem.IsMacOSVersionAtLeast(15) ||
				OperatingSystem.IsMacCatalystVersionAtLeast(18)
					? NSCursor.ZoomInCursor
					: NSCursor.ArrowCursor,
			CursorIcon.ZoomOut =>
				OperatingSystem.IsMacOSVersionAtLeast(15) ||
				OperatingSystem.IsMacCatalystVersionAtLeast(18)
					? NSCursor.ZoomOutCursor
					: NSCursor.ArrowCursor,
			CursorIcon.Copy => NSCursor.DragCopyCursor,
			CursorIcon.Alias => NSCursor.DragLinkCursor,
			_ => NSCursor.ArrowCursor,
		};
	}

	class PointerUIHoverGestureRecognizer(Action<UIHoverGestureRecognizer> action)
		: UIHoverGestureRecognizer(action)
	{
	}
}
