using System;
using System.Windows.Input;

namespace ReadX.Models;

public sealed record AppSettings
{
    public int DefaultWpm { get; init; } = 300;
    public bool CleanupEnabled { get; init; } = true;
    public int HistoryLimit { get; init; } = 20;
    public HotkeyBinding CaptureHotkey { get; init; } = new(ModifierKeys.Control | ModifierKeys.Shift, Key.R);
    public HotkeyBinding ReplayLastHotkey { get; init; } = new(ModifierKeys.Control | ModifierKeys.Shift, Key.E);
    public HotkeyBinding PauseResumeHotkey { get; init; } = new(ModifierKeys.None, Key.Space);
    public HotkeyBinding CancelHotkey { get; init; } = new(ModifierKeys.None, Key.Escape);
    public double WindowWidth { get; init; } = 860;
    public double WindowHeight { get; init; } = 560;
    public double WindowLeft { get; init; } = double.NaN;
    public double WindowTop { get; init; } = double.NaN;
    public bool WindowMaximized { get; init; }

    public static AppSettings CreateDefault() => new();

    public AppSettings Normalized() => this with
    {
        DefaultWpm = Math.Clamp(DefaultWpm, Services.RsvpPlayer.MinimumWpm, Services.RsvpPlayer.MaximumWpm),
        HistoryLimit = Math.Clamp(HistoryLimit, 1, 500),
        WindowWidth = !double.IsFinite(WindowWidth) || WindowWidth < 680 ? 860 : WindowWidth,
        WindowHeight = !double.IsFinite(WindowHeight) || WindowHeight < 480 ? 560 : WindowHeight,
        WindowLeft = double.IsFinite(WindowLeft) || double.IsNaN(WindowLeft) ? WindowLeft : double.NaN,
        WindowTop = double.IsFinite(WindowTop) || double.IsNaN(WindowTop) ? WindowTop : double.NaN
    };
}
