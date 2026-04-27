using System.Text.Json.Serialization;
using System.Windows.Input;

namespace ReadX.Models;

public sealed record HotkeyBinding(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ModifierKeys Modifiers,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Key Key)
{
    public string ToDisplayText()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        parts.Add(Key == Key.Escape ? "Esc" : Key.ToString());
        return string.Join("+", parts);
    }
}
