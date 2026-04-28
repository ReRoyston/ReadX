using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ReadX.Models;

namespace ReadX.Services;

public sealed class HotkeyService : IHotkeyService
{
    private const int BaseHotkeyId = 0x5258;
    private const int WmHotkey = 0x0312;

    private readonly Dictionary<int, HotkeyAction> actionsById = [];
    private IntPtr hwnd;
    private HwndSource? source;
    private Action<HotkeyAction>? callback;
    private bool isBusy;

    public IReadOnlyList<HotkeyRegistration> RegisterAll(
        IntPtr hwnd,
        IReadOnlyDictionary<HotkeyAction, HotkeyBinding> bindings,
        Action<HotkeyAction> callback)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(hwnd));
        }

        ArgumentNullException.ThrowIfNull(callback);

        Unregister();

        this.hwnd = hwnd;
        this.callback = callback;
        source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        var registrations = new List<HotkeyRegistration>(bindings.Count);
        foreach (var pair in bindings.OrderBy(pair => pair.Key))
        {
            var id = BaseHotkeyId + (int)pair.Key;
            var isRegistered = TryRegisterWin32Hotkey(hwnd, id, pair.Value);

            if (isRegistered)
            {
                actionsById[id] = pair.Key;
            }

            registrations.Add(new HotkeyRegistration(pair.Key, pair.Value, isRegistered));
        }

        return registrations;
    }

    public bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var registrations = RegisterAll(
            hwnd,
            new Dictionary<HotkeyAction, HotkeyBinding>
            {
                [HotkeyAction.Capture] = new(mods, key)
            },
            action =>
            {
                if (action == HotkeyAction.Capture)
                {
                    callback();
                }
            });

        return registrations.Count == 1 && registrations[0].IsRegistered;
    }

    public void Unregister()
    {
        if (source is not null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }

        foreach (var id in actionsById.Keys.ToArray())
        {
            UnregisterHotKey(hwnd, id);
        }

        actionsById.Clear();
        hwnd = IntPtr.Zero;
        callback = null;
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
        if (message == WmHotkey && actionsById.TryGetValue(wParam.ToInt32(), out var action))
        {
            handled = true;

            if (!isBusy || action is HotkeyAction.PauseResume or HotkeyAction.Cancel)
            {
                callback?.Invoke(action);
            }
        }

        return IntPtr.Zero;
    }

    private static bool TryRegisterWin32Hotkey(IntPtr hwnd, int id, HotkeyBinding binding)
    {
        try
        {
            return RegisterHotKey(
                hwnd,
                id,
                HotkeyMapping.ToModifierFlags(binding.Modifiers),
                HotkeyMapping.ToVirtualKey(binding.Key));
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
