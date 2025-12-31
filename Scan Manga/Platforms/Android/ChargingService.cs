using Android.Content;
using Android.OS;
using Scan_Manga.Services;
using AndroidApp = Android.App;

namespace Scan_Manga.Platforms.Android;

public class ChargingService : IChargingService
{
    public event EventHandler<bool>? ChargingStateChanged;
    private bool _isCharging;

    public ChargingService()
    {
        var filter = new IntentFilter(Intent.ActionBatteryChanged);
        AndroidApp.Application.Context.RegisterReceiver(new BatteryReceiver(this), filter);
    }

    public bool IsCharging => _isCharging;

    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class BatteryReceiver : BroadcastReceiver
    {
        private readonly ChargingService _service;
    
        public BatteryReceiver(ChargingService service) => _service = service;

        public BatteryReceiver() => _service = new();

        public override void OnReceive(Context? context, Intent? intent)
        {
            var status = intent?.GetIntExtra(BatteryManager.ExtraStatus, -1);
            var charging = status == (int)BatteryStatus.Charging || status == (int)BatteryStatus.Full;
            if (charging != _service._isCharging)
            {
                _service._isCharging = charging;
                _service.ChargingStateChanged?.Invoke(_service, charging);
            }
        }
    }
}
