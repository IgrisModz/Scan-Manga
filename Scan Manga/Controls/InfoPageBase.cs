using Scan_Manga.Services;

namespace Scan_Manga.Controls;

public class InfoPageBase : ContentPage
{
    private readonly IFullScreenService _fullScreenService;


    public InfoPageBase(IFullScreenService fullScreenService)
    {
        _fullScreenService = fullScreenService;
    }

    public InfoPageBase() : this(ServiceHelper.Services.GetRequiredService<IFullScreenService>())
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _fullScreenService.ExitFullScreen();
    }
}
