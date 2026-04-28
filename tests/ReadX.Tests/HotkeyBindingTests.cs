using System.Windows.Input;
using ReadX.Models;

namespace ReadX.Tests;

public sealed class HotkeyBindingTests
{
    [Fact]
    public void ToDisplayText_IncludesModifiersAndKey()
    {
        var binding = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Shift, Key.R);

        Assert.Equal("Ctrl+Shift+R", binding.ToDisplayText());
    }

    [Fact]
    public void ToDisplayText_ForEscape_UsesEscLabel()
    {
        var binding = new HotkeyBinding(ModifierKeys.None, Key.Escape);

        Assert.Equal("Esc", binding.ToDisplayText());
    }

    [Fact]
    public void CreateDefault_ReturnsApprovedV2Bindings()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal("Ctrl+Shift+R", settings.CaptureHotkey.ToDisplayText());
        Assert.Equal("Ctrl+Shift+E", settings.ReplayLastHotkey.ToDisplayText());
        Assert.Equal("Space", settings.PauseResumeHotkey.ToDisplayText());
        Assert.Equal("Esc", settings.CancelHotkey.ToDisplayText());
    }
}
