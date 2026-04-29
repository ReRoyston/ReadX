using System.Windows.Input;
using ReadX.Models;

namespace ReadX.Services;

public enum HotkeyAction
{
    Capture,
    ReplayLast,
    PauseResume,
    Cancel
}

public sealed record HotkeyRegistration(HotkeyAction Action, HotkeyBinding Binding, bool IsRegistered);

public interface IHotkeyService : IDisposable
{
    IReadOnlyList<HotkeyRegistration> RegisterAll(
        IntPtr hwnd,
        IReadOnlyDictionary<HotkeyAction, HotkeyBinding> bindings,
        Action<HotkeyAction> callback);

    bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback);
    void Unregister();
    void SetBusy(bool busy);
}
