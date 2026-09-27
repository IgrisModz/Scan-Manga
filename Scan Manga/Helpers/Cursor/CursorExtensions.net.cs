using System;
using System.Collections.Generic;
using System.Text;

namespace Scan_Manga.Helpers;

public static partial class CursorExtensions
{
	public static void SetCustomCursor(
		this VisualElement visualElement,
		CursorIcon cursor,
		IMauiContext? mauiContext)
	{
		throw new PlatformNotSupportedException(
			$"Custom cursors are not supported on {DeviceInfo.Platform} platform."););
	}
}
