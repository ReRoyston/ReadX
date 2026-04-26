using System.Windows.Input;

namespace ReadX.Services;

public interface IHotkeyService : IDisposable
{
    bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback);
    void Unregister();
    void SetBusy(bool busy);
}
