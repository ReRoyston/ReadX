using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace ReadX.Services;

public sealed class OcrService : IOcrService
{
    private readonly TesseractEngine engine;
    private bool isDisposed;

    public OcrService(string tessdataPath, string language = "eng")
    {
        if (string.IsNullOrWhiteSpace(tessdataPath))
        {
            throw new ArgumentException("Tessdata path is required.", nameof(tessdataPath));
        }

        if (!Directory.Exists(tessdataPath))
        {
            throw new DirectoryNotFoundException($"Tessdata directory was not found: {tessdataPath}");
        }

        var trainedDataPath = Path.Combine(tessdataPath, $"{language}.traineddata");
        if (!File.Exists(trainedDataPath))
        {
            throw new FileNotFoundException("Tesseract language data file was not found.", trainedDataPath);
        }

        engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
    }

    public string Recognise(Bitmap image)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(image);

        using var pix = CreatePix(image);
        using var page = engine.Process(pix, PageSegMode.Auto);
        return page.GetText()?.Trim() ?? string.Empty;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        engine.Dispose();
        isDisposed = true;
    }

    private static Pix CreatePix(Bitmap image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return Pix.LoadFromMemory(stream.ToArray());
    }
}
