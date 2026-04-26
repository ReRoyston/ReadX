using System.Drawing;
using ReadX.Models;

namespace ReadX.Services;

public interface IScreenCaptureService
{
    Bitmap Capture(CaptureRegion region);
}
