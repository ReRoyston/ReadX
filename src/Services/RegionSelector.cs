using ReadX.Models;
using ReadX.Views;

namespace ReadX.Services;

public sealed class RegionSelector : IRegionSelector
{
    public Task<CaptureRegion?> SelectAsync()
    {
        var overlay = new RegionSelectOverlay();
        return overlay.SelectAsync();
    }
}
