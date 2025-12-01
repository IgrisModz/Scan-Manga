using Scan_Manga.Services;

namespace Scan_Manga.Controls;

public class InfoPageBase(IFullScreenService fullScreenService) : ContentPage
{
    private readonly IFullScreenService _fullScreenService = fullScreenService;

    public InfoPageBase() : this(ServiceHelper.Services.GetRequiredService<IFullScreenService>())
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _fullScreenService.ExitFullScreen();
    }
}
