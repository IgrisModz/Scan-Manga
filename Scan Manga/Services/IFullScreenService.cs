namespace Scan_Manga.Services;

public interface IFullScreenService
{
    void SetFullScreen(bool enable);
    void EnterFullScreen();
    void ExitFullScreen();
}
