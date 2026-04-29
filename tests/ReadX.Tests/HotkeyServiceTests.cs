using System.Windows.Input;
using System.Windows.Interop;
using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests;

public sealed class HotkeyServiceTests
{
    private const int BaseHotkeyId = 0x5258;
    private static readonly IntPtr Hwnd = new(123);

    [Fact]
    public void RegisterAll_AssignsStableIdsAndTracksOnlySuccessfulRegistrations()
    {
        var native = new FakeHotkeyNativeMethods();
        native.RegisterResults[IdFor(HotkeyAction.ReplayLast)] = false;
        using var service = new HotkeyService(native);
        var callbacks = new List<HotkeyAction>();
        var bindings = CreateBindings();

        var registrations = service.RegisterAll(Hwnd, bindings, callbacks.Add);

        Assert.Equal(
            [
                IdFor(HotkeyAction.Capture),
                IdFor(HotkeyAction.ReplayLast)
            ],
            native.RegisterCalls.Select(call => call.Id));
        Assert.Equal(Hwnd, native.RegisterCalls[0].Hwnd);
        Assert.Equal(0x0006u, native.RegisterCalls[0].Modifiers);
        Assert.Equal((uint)KeyInterop.VirtualKeyFromKey(Key.R), native.RegisterCalls[0].Key);
        Assert.Equal(
            [
                new HotkeyRegistration(HotkeyAction.Capture, bindings[HotkeyAction.Capture], true),
                new HotkeyRegistration(HotkeyAction.ReplayLast, bindings[HotkeyAction.ReplayLast], false),
                new HotkeyRegistration(HotkeyAction.PauseResume, bindings[HotkeyAction.PauseResume], false),
                new HotkeyRegistration(HotkeyAction.Cancel, bindings[HotkeyAction.Cancel], false)
            ],
            registrations);

        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.Capture)));
        Assert.False(service.HandleHotkeyMessage(IdFor(HotkeyAction.ReplayLast)));
        Assert.Equal([HotkeyAction.Capture], callbacks);
    }

    [Fact]
    public void Unregister_UnregistersSuccessfulIdsOnly()
    {
        var native = new FakeHotkeyNativeMethods();
        native.RegisterResults[IdFor(HotkeyAction.ReplayLast)] = false;
        using var service = new HotkeyService(native);
        service.RegisterAll(Hwnd, CreateBindings(), _ => { });

        service.Unregister();

        Assert.Equal(
            [
                IdFor(HotkeyAction.Capture)
            ],
            native.UnregisterCalls.Select(call => call.Id));
        Assert.All(native.UnregisterCalls, call => Assert.Equal(Hwnd, call.Hwnd));
    }

    [Fact]
    public void TryRegister_RegistersCaptureOnlyAndInvokesLegacyCallbackOnlyForCapture()
    {
        var native = new FakeHotkeyNativeMethods();
        using var service = new HotkeyService(native);
        var callbackCount = 0;

        var registered = service.TryRegister(Hwnd, ModifierKeys.Control | ModifierKeys.Shift, Key.R, () => callbackCount++);

        Assert.True(registered);
        var call = Assert.Single(native.RegisterCalls);
        Assert.Equal(IdFor(HotkeyAction.Capture), call.Id);
        Assert.Equal(0x0006u, call.Modifiers);
        Assert.Equal((uint)KeyInterop.VirtualKeyFromKey(Key.R), call.Key);
        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.Capture)));
        Assert.False(service.HandleHotkeyMessage(IdFor(HotkeyAction.ReplayLast)));
        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void HandleHotkeyMessage_WhenBusy_SuppressesCaptureAndReplayButAllowsPauseAndCancel()
    {
        var native = new FakeHotkeyNativeMethods();
        using var service = new HotkeyService(native);
        var callbacks = new List<HotkeyAction>();
        service.RegisterAll(Hwnd, CreateBindings(), callbacks.Add);
        service.SetBusy(true);

        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.Capture)));
        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.ReplayLast)));
        Assert.False(service.HandleHotkeyMessage(IdFor(HotkeyAction.PauseResume)));
        Assert.False(service.HandleHotkeyMessage(IdFor(HotkeyAction.Cancel)));

        Assert.Empty(callbacks);
    }

    [Fact]
    public void RegisterAll_WhenOneBindingFails_StillDispatchesOtherSuccessfulActions()
    {
        var native = new FakeHotkeyNativeMethods();
        native.RegisterResults[IdFor(HotkeyAction.Capture)] = false;
        using var service = new HotkeyService(native);
        var callbacks = new List<HotkeyAction>();

        var registrations = service.RegisterAll(Hwnd, CreateBindings(), callbacks.Add);

        Assert.False(registrations.Single(registration => registration.Action == HotkeyAction.Capture).IsRegistered);
        Assert.True(registrations.Single(registration => registration.Action == HotkeyAction.ReplayLast).IsRegistered);
        Assert.False(registrations.Single(registration => registration.Action == HotkeyAction.PauseResume).IsRegistered);
        Assert.False(registrations.Single(registration => registration.Action == HotkeyAction.Cancel).IsRegistered);
        Assert.False(service.HandleHotkeyMessage(IdFor(HotkeyAction.Capture)));
        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.ReplayLast)));
        Assert.Equal([HotkeyAction.ReplayLast], callbacks);
    }

    [Fact]
    public void RegisterAll_WhenPauseAndCancelHaveModifiers_RegistersThemAsGlobalHotkeys()
    {
        var native = new FakeHotkeyNativeMethods();
        using var service = new HotkeyService(native);
        var callbacks = new List<HotkeyAction>();
        var bindings = CreateBindings();
        bindings[HotkeyAction.PauseResume] = new HotkeyBinding(ModifierKeys.Control, Key.Space);
        bindings[HotkeyAction.Cancel] = new HotkeyBinding(ModifierKeys.Control, Key.Escape);

        var registrations = service.RegisterAll(Hwnd, bindings, callbacks.Add);
        service.SetBusy(true);

        Assert.True(registrations.Single(registration => registration.Action == HotkeyAction.PauseResume).IsRegistered);
        Assert.True(registrations.Single(registration => registration.Action == HotkeyAction.Cancel).IsRegistered);
        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.PauseResume)));
        Assert.True(service.HandleHotkeyMessage(IdFor(HotkeyAction.Cancel)));
        Assert.Equal([HotkeyAction.PauseResume, HotkeyAction.Cancel], callbacks);
    }

    private static Dictionary<HotkeyAction, HotkeyBinding> CreateBindings() => new()
    {
        [HotkeyAction.Capture] = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Shift, Key.R),
        [HotkeyAction.ReplayLast] = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Shift, Key.E),
        [HotkeyAction.PauseResume] = new HotkeyBinding(ModifierKeys.None, Key.Space),
        [HotkeyAction.Cancel] = new HotkeyBinding(ModifierKeys.None, Key.Escape)
    };

    private static int IdFor(HotkeyAction action) => BaseHotkeyId + (int)action;

    private sealed class FakeHotkeyNativeMethods : IHotkeyNativeMethods
    {
        public Dictionary<int, bool> RegisterResults { get; } = [];
        public List<RegisterCall> RegisterCalls { get; } = [];
        public List<UnregisterCall> UnregisterCalls { get; } = [];

        public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key)
        {
            RegisterCalls.Add(new RegisterCall(hwnd, id, modifiers, key));
            return !RegisterResults.TryGetValue(id, out var result) || result;
        }

        public bool UnregisterHotKey(IntPtr hwnd, int id)
        {
            UnregisterCalls.Add(new UnregisterCall(hwnd, id));
            return true;
        }
    }

    private sealed record RegisterCall(IntPtr Hwnd, int Id, uint Modifiers, uint Key);
    private sealed record UnregisterCall(IntPtr Hwnd, int Id);
}
