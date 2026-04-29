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
    private readonly IHotkeyNativeMethods nativeMethods;
    private readonly bool addMessageHook;
    private IntPtr hwnd;
    private HwndSource? source;
    private Action<HotkeyAction>? callback;
    private bool isBusy;

    public HotkeyService()
        : this(new HotkeyNativeMethods(), addMessageHook: true)
    {
    }

    internal HotkeyService(IHotkeyNativeMethods nativeMethods)
        : this(nativeMethods, addMessageHook: false)
    {
    }

    private HotkeyService(IHotkeyNativeMethods nativeMethods, bool addMessageHook)
    {
        this.nativeMethods = nativeMethods;
        this.addMessageHook = addMessageHook;
    }

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
        if (addMessageHook)
        {
            source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }

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
            nativeMethods.UnregisterHotKey(hwnd, id);
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
        if (message == WmHotkey)
        {
            handled = HandleHotkeyMessage(wParam.ToInt32());
        }

        return IntPtr.Zero;
    }

    internal bool HandleHotkeyMessage(int id)
    {
        if (!actionsById.TryGetValue(id, out var action))
        {
            return false;
        }

        if (!isBusy || action is HotkeyAction.PauseResume or HotkeyAction.Cancel)
        {
            callback?.Invoke(action);
        }

        return true;
    }

    private bool TryRegisterWin32Hotkey(IntPtr hwnd, int id, HotkeyBinding binding)
    {
        if (binding.Modifiers == ModifierKeys.None)
        {
            return false;
        }

        try
        {
            return nativeMethods.RegisterHotKey(
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
}

internal interface IHotkeyNativeMethods
{
    bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    bool UnregisterHotKey(IntPtr hwnd, int id);
}

internal sealed class HotkeyNativeMethods : IHotkeyNativeMethods
{
    public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key)
    {
        return RegisterHotKeyNative(hwnd, id, modifiers, key);
    }

    public bool UnregisterHotKey(IntPtr hwnd, int id)
    {
        return UnregisterHotKeyNative(hwnd, id);
    }

    [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
    private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
    private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);
}
