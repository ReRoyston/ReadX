using System.Drawing;
using System.Windows.Input;
using ReadX.Models;
using ReadX.Services;
using ReadX.Tests.Fakes;

namespace ReadX.Tests;

public class AppControllerTests
{
    [Fact]
    public async Task ImportTextAsync_StoresRawTextUsesCleanupPipelineAndStartsPlayback()
    {
        var settings = AppSettings.CreateDefault() with { CleanupEnabled = true, HistoryLimit = 5 };
        var history = new InMemoryHistoryStore();
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(settings, history, presenter);

        await controller.ImportTextAsync("recom-\nmend   this");

        Assert.Equal(settings, controller.Settings);
        Assert.Equal(["recom-\nmend   this"], controller.History.Select(item => item.RawText));
        Assert.Equal(HistorySource.Import, controller.History[0].Source);
        Assert.NotNull(controller.LastSession);
        Assert.Equal(["recommend", "this"], controller.LastSession.Words);
        Assert.Equal("recom-\nmend   this", controller.LastSession.RawText);
        Assert.Equal("recommend this", controller.LastSession.ProcessedText);
        Assert.Equal(HistorySource.Import, controller.LastSession.Source);
        Assert.Null(presenter.Regions.Single());
        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("Ready.", controller.Status);
    }

    [Fact]
    public async Task ImportTextAsync_WhenCleanupDisabledPreservesRawProcessedTextButStillTokenizes()
    {
        var settings = AppSettings.CreateDefault() with { CleanupEnabled = false };
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(settings, new InMemoryHistoryStore(), presenter);

        await controller.ImportTextAsync("recom-\nmend   this");

        Assert.NotNull(controller.LastSession);
        Assert.Equal("recom-\nmend   this", controller.LastSession.ProcessedText);
        Assert.Equal(["recom-", "mend", "this"], controller.LastSession.Words);
        Assert.Equal(1, presenter.PlayCount);
    }

    [Fact]
    public async Task ReplayHistoryAsync_StartsPlaybackWithoutAddingHistory()
    {
        var item = new HistoryItem(Guid.NewGuid(), DateTimeOffset.UtcNow, HistorySource.Import, "one two");
        var history = new InMemoryHistoryStore();
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(
            AppSettings.CreateDefault(),
            history,
            presenter,
            loadedHistory: [item]);

        await controller.ReplayHistoryAsync(item);

        Assert.Equal(0, history.AddCount);
        Assert.Equal(["one", "two"], controller.LastSession!.Words);
        Assert.Equal(HistorySource.Import, controller.LastSession.Source);
        Assert.Null(presenter.Regions.Single());
    }

    [Fact]
    public async Task StartCaptureAsync_UsesPipelineAndAddsRawOcrTextToHistory()
    {
        var settings = AppSettings.CreateDefault() with { CleanupEnabled = true, HistoryLimit = 7 };
        var region = new CaptureRegion(10, 20, 120, 40);
        var history = new InMemoryHistoryStore();
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(
            settings,
            history,
            presenter,
            selector: new FakeRegionSelector(region),
            capture: new FakeScreenCaptureService(),
            ocr: new FakeOcrService("recom-\nmend   this"));

        await controller.StartCaptureAsync();

        Assert.Equal([(RawText: "recom-\nmend   this", Source: HistorySource.Capture, Limit: 7)], history.Adds);
        Assert.Equal(["recom-\nmend   this"], controller.History.Select(item => item.RawText));
        Assert.Equal(["recommend", "this"], controller.LastSession!.Words);
        Assert.Equal(region, controller.LastSession.Region);
        Assert.Equal("recommend this", controller.LastSession.ProcessedText);
        Assert.Equal(region, presenter.Regions.Single());
    }

    [Fact]
    public async Task ReplayLastAsync_ReplaysImportSessionWithoutCaptureRegion()
    {
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), new InMemoryHistoryStore(), presenter);

        await controller.ImportTextAsync("one two");
        await controller.ReplayLastAsync();

        Assert.Equal(2, presenter.PlayCount);
        Assert.All(presenter.Regions, Assert.Null);
        Assert.Equal("Ready.", controller.Status);
    }

    [Fact]
    public async Task ImportTextAsync_WhenRawTextIsWhitespace_DoesNotStoreAndLeavesIdleStatus()
    {
        var history = new InMemoryHistoryStore();
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), history, presenter);

        await controller.ImportTextAsync(" \r\n ");

        Assert.Equal(0, history.AddCount);
        Assert.Empty(controller.History);
        Assert.Null(controller.LastSession);
        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("No text found.", controller.Status);
        Assert.Equal(0, presenter.PlayCount);
    }

    [Fact]
    public async Task PlaybackControlsPauseRestartAndCancelActivePlayback()
    {
        var presenter = new FakeRsvpPresenter { HoldPlaybackOpen = true };
        var ticker = new FakeTicker();
        var controller = CreateController(
            AppSettings.CreateDefault(),
            new InMemoryHistoryStore(),
            presenter,
            player: new RsvpPlayer(ticker));

        var playback = controller.ImportTextAsync("one two");
        await presenter.WaitForPlaybackAsync();

        controller.PauseOrResumePlayback();
        Assert.Equal(PlayerState.Paused, presenter.LastPlayer!.State);
        Assert.False(ticker.IsRunning);

        controller.PauseOrResumePlayback();
        Assert.Equal(PlayerState.Playing, presenter.LastPlayer.State);
        Assert.True(ticker.IsRunning);

        ticker.Pulse();
        Assert.Equal(0, presenter.LastPlayer.Index);

        controller.RestartPlayback();
        Assert.Equal(PlayerState.Playing, presenter.LastPlayer.State);
        Assert.Equal(-1, presenter.LastPlayer.Index);
        Assert.True(ticker.IsRunning);

        controller.CancelPlayback();

        Assert.Equal(PlayerState.Idle, presenter.LastPlayer.State);
        Assert.False(ticker.IsRunning);
        Assert.Equal(1, presenter.CloseCount);
        Assert.Equal(AppState.Idle, controller.State);
        await playback;
    }

    private static AppController CreateController(
        AppSettings settings,
        InMemoryHistoryStore history,
        FakeRsvpPresenter presenter,
        IReadOnlyList<HistoryItem>? loadedHistory = null,
        IRegionSelector? selector = null,
        IScreenCaptureService? capture = null,
        IOcrService? ocr = null,
        RsvpPlayer? player = null)
    {
        return new AppController(
            new FakeHotkeyService(),
            selector ?? new FakeRegionSelector(),
            capture ?? new FakeScreenCaptureService(),
            ocr,
            player ?? new RsvpPlayer(new FakeTicker()),
            presenter,
            new InMemorySettingsStore(settings),
            history,
            settings,
            loadedHistory ?? []);
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

    private sealed class FakeRegionSelector(CaptureRegion? region = null) : IRegionSelector
    {
        public Task<CaptureRegion?> SelectAsync()
        {
            return Task.FromResult(region);
        }
    }

    private sealed class FakeScreenCaptureService : IScreenCaptureService
    {
        public Bitmap Capture(CaptureRegion region)
        {
            return new Bitmap(1, 1);
        }
    }

    private sealed class FakeOcrService(string text) : IOcrService
    {
        public string Recognise(Bitmap image)
        {
            return text;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeRsvpPresenter : IRsvpPresenter
    {
        private readonly TaskCompletionSource playbackStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource? activePlayback;

        public bool HoldPlaybackOpen { get; init; }
        public int PlayCount { get; private set; }
        public int CloseCount { get; private set; }
        public RsvpPlayer? LastPlayer { get; private set; }
        public List<CaptureRegion?> Regions { get; } = [];

        public Task PlayAsync(RsvpPlayer player, CaptureRegion? region)
        {
            PlayCount++;
            LastPlayer = player;
            Regions.Add(region);
            player.Start();
            playbackStarted.TrySetResult();

            if (!HoldPlaybackOpen)
            {
                return Task.CompletedTask;
            }

            activePlayback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return activePlayback.Task;
        }

        public Task WaitForPlaybackAsync()
        {
            return playbackStarted.Task;
        }

        public void Close()
        {
            CloseCount++;
            activePlayback?.TrySetResult();
        }
    }
}
