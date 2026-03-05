using Scan_Manga.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scan_Manga.Platforms.iOS;

public class ChargingService : IChargingService
{
    public bool IsCharging => throw new NotSupportedException();

    public event EventHandler<bool> ChargingStateChanged;
}
