namespace ReadX.Models;

public sealed record RsvpSession(IReadOnlyList<string> Words, CaptureRegion Region, string Text);
