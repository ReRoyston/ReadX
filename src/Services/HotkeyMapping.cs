using System.Windows.Input;
using System.Windows.Interop;

namespace ReadX.Services;

public static class HotkeyMapping
{
    public static uint ToModifierFlags(ModifierKeys modifiers)
    {
        uint flags = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            flags |= 0x0001;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            flags |= 0x0002;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            flags |= 0x0004;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            flags |= 0x0008;
        }

        return flags;
    }

    public static uint ToVirtualKey(Key key)
    {
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, "Key cannot be mapped to a Win32 virtual key.");
        }

        return (uint)virtualKey;
    }
}
