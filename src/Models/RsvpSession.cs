namespace ReadX.Models;

public sealed record RsvpSession(
    IReadOnlyList<string> Words,
    CaptureRegion? Region,
    string RawText,
    string ProcessedText,
    HistorySource Source)
{
    public string Text => RawText;

    public RsvpSession(IReadOnlyList<string> words, CaptureRegion region, string text)
        : this(words, region, text, text, HistorySource.Capture)
    {
    }
}
