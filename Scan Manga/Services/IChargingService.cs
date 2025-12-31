namespace Scan_Manga.Services;

public interface IChargingService
{
    event EventHandler<bool> ChargingStateChanged;
    bool IsCharging { get; }
}
