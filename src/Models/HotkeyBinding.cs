using System.Text.Json.Serialization;
using System.Windows.Input;

namespace ReadX.Models;

public sealed record HotkeyBinding(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ModifierKeys Modifiers,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Key Key)
{
    public static bool TryParseDisplayText(string? text, out HotkeyBinding binding)
    {
        binding = new HotkeyBinding(ModifierKeys.None, Key.None);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var modifiers = ModifierKeys.None;
        Key? key = null;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Windows;
                    break;
                case "esc":
                case "escape":
                    key = Key.Escape;
                    break;
                default:
                    if (!Enum.TryParse<Key>(part, ignoreCase: true, out var parsedKey) || parsedKey == Key.None)
                    {
                        return false;
                    }

                    key = parsedKey;
                    break;
            }
        }

        if (key is null)
        {
            return false;
        }

        binding = new HotkeyBinding(modifiers, key.Value);
        return true;
    }

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
