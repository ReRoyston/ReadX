namespace ReadX.Models;

// Physical pixels relative to the virtual-screen origin.
public sealed record CaptureRegion(int X, int Y, int Width, int Height);
