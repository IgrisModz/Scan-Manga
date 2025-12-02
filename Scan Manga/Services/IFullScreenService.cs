namespace Scan_Manga.Services;

public interface IFullScreenService
{
    bool IsFullScreen { get; set; }

    void SetFullScreen(bool enable);
    void EnterFullScreen();
    void ExitFullScreen();
}
