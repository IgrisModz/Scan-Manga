namespace Scan_Manga.Services;

public enum SystemBarsMode
{
    Default,
    Lecture
}

public interface ISystemBarsService
{
    SystemBarsMode CurrentMode { get; }

    /// <summary>
    /// Active ou désactive le mode lecture (barres cachées/translucides).
    /// </summary>
    /// <param name="enable">true = cacher les barres pour lecture plein écran, false = affichage normal</param>
    void SetLectureMode(bool enable);
}
