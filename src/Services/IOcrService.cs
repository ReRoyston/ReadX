using System.Drawing;

namespace ReadX.Services;

public interface IOcrService : IDisposable
{
    string Recognise(Bitmap image);
}
