using System.Runtime.Versioning;
using Android.Views;
using Microsoft.Maui.Platform;
using Application = Android.App.Application;

namespace Scan_Manga.Helpers;

public static partial class CursorExtensions
{
	public static void SetCustomCursor(this VisualElement visualElement, CursorIcon cursor, IMauiContext? mauiContext)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(24))
		{
			ArgumentNullException.ThrowIfNull(mauiContext);
			var view = visualElement.ToPlatform(mauiContext);
			view.PointerIcon = PointerIcon.GetSystemIcon(Application.Context, GetCursor(cursor));
		}
	}

	[SupportedOSPlatform("android24.0")]
	static PointerIconType GetCursor(CursorIcon cursor)
	{
		return cursor switch
		{
			CursorIcon.Arrow => PointerIconType.Arrow,
			CursorIcon.Hand => PointerIconType.Hand,
			CursorIcon.IBeam => PointerIconType.Text,
			CursorIcon.Cross => PointerIconType.Crosshair,
			CursorIcon.Wait => PointerIconType.Wait,
			CursorIcon.NotAllowed => PointerIconType.NoDrop,
			CursorIcon.Help => PointerIconType.Help,
			CursorIcon.SizeAll => PointerIconType.AllScroll,
			CursorIcon.SizeHorizontal => PointerIconType.HorizontalDoubleArrow,
			CursorIcon.SizeVertical => PointerIconType.VerticalDoubleArrow,
			CursorIcon.SizeDiagonalNWSE => PointerIconType.TopLeftDiagonalDoubleArrow,
			CursorIcon.SizeDiagonalNESW => PointerIconType.TopRightDiagonalDoubleArrow,
			_ => PointerIconType.Default,
		};
	}
}
