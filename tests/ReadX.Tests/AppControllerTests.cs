using System.Drawing;
using System.Windows.Input;
using ReadX.Models;
using ReadX.Services;
using ReadX.Tests.Fakes;

namespace ReadX.Tests;

public class AppControllerTests
{
    [Fact]
    public async Task ReplayLastAsync_WhenSessionHasNoRegion_DoesNotPlayAndReportsStatus()
    {
        var hotkey = new FakeHotkeyService();
        var presenter = new FakeRsvpPresenter();
        var controller = new AppController(
            hotkey,
            new FakeRegionSelector(),
            new FakeScreenCaptureService(),
            ocr: null,
            new RsvpPlayer(new FakeTicker()),
            presenter);
        SetLastSession(controller, new RsvpSession(["one"], null, "one", "one", HistorySource.Import));

        await controller.ReplayLastAsync();

        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("No capture region available for replay.", controller.Status);
        Assert.Equal(0, presenter.PlayCount);
        Assert.Empty(hotkey.BusyValues);
    }

    private static void SetLastSession(AppController controller, RsvpSession session)
    {
        typeof(AppController)
            .GetProperty(nameof(AppController.LastSession))!
            .SetValue(controller, session);
    }

    private sealed class FakeHotkeyService : IHotkeyService
    {
        public List<bool> BusyValues { get; } = [];

        public IReadOnlyList<HotkeyRegistration> RegisterAll(
            IntPtr hwnd,
            IReadOnlyDictionary<HotkeyAction, HotkeyBinding> bindings,
            Action<HotkeyAction> callback)
        {
            return bindings
                .Select(pair => new HotkeyRegistration(pair.Key, pair.Value, true))
                .ToArray();
        }

        public bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback)
        {
            return true;
        }

        public void Unregister()
        {
        }

        public void SetBusy(bool busy)
        {
            BusyValues.Add(busy);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeRegionSelector : IRegionSelector
    {
        public Task<CaptureRegion?> SelectAsync()
        {
            return Task.FromResult<CaptureRegion?>(null);
        }
    }

    private sealed class FakeScreenCaptureService : IScreenCaptureService
    {
        public Bitmap Capture(CaptureRegion region)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeRsvpPresenter : IRsvpPresenter
    {
        public int PlayCount { get; private set; }

        public Task PlayAsync(RsvpPlayer player, CaptureRegion region)
        {
            PlayCount++;
            return Task.CompletedTask;
        }

        public void Close()
        {
        }
    }
}
