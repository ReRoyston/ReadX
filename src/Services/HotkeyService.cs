using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ReadX.Services;

public sealed class HotkeyService : IHotkeyService
{
    private const int HotkeyId = 0x5258;
    private const int WmHotkey = 0x0312;

    private IntPtr hwnd;
    private HwndSource? source;
    private Action? callback;
    private bool isBusy;
    private bool isRegistered;

    public bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(hwnd));
        }

        ArgumentNullException.ThrowIfNull(callback);

        Unregister();

        var modifierFlags = HotkeyMapping.ToModifierFlags(mods);
        var virtualKey = HotkeyMapping.ToVirtualKey(key);

        if (!RegisterHotKey(hwnd, HotkeyId, modifierFlags, virtualKey))
        {
            return false;
        }

        this.hwnd = hwnd;
        this.callback = callback;
        source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
        isRegistered = true;

        return true;
    }

    public void Unregister()
    {
        if (source is not null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }

        if (isRegistered)
        {
            UnregisterHotKey(hwnd, HotkeyId);
        }

        hwnd = IntPtr.Zero;
        callback = null;
        isRegistered = false;
        isBusy = false;
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;
    }

    public void Dispose()
    {
        Unregister();
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;

            if (!isBusy)
            {
                callback?.Invoke();
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
