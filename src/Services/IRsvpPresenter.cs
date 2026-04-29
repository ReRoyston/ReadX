using ReadX.Models;

namespace ReadX.Services;

public interface IRsvpPresenter
{
    Task PlayAsync(RsvpPlayer player, CaptureRegion? region);
    void Close();
}
