using ReadX.Models;

namespace ReadX.Services;

public interface IRegionSelector
{
    Task<CaptureRegion?> SelectAsync();
}
