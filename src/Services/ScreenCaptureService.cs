using System.Drawing;
using ReadX.Models;

namespace ReadX.Services;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    public Bitmap Capture(CaptureRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Capture region must have positive dimensions.");
        }

        var bitmap = new Bitmap(region.Width, region.Height);

        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;
            graphics.CopyFromScreen(virtualScreen.Left + region.X, virtualScreen.Top + region.Y, 0, 0, bitmap.Size);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
