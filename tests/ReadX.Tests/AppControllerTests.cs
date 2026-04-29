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
    public async Task ImportTextAsync_WhenHistoryAddFails_ReturnsIdleAndDoesNotPlay()
    {
        var history = new InMemoryHistoryStore
        {
            AddException = new IOException("store unavailable")
        };
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), history, presenter);

        await controller.ImportTextAsync("one two");

        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("History update failed.", controller.Status);
        Assert.Null(controller.LastSession);
        Assert.Empty(controller.History);
        Assert.Equal(0, presenter.PlayCount);
    }

    [Fact]
    public async Task ImportTextAsync_WhenHistoryLoadFails_ReturnsIdleAndDoesNotPlay()
    {
        var history = new InMemoryHistoryStore
        {
            LoadException = new IOException("history unreadable")
        };
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), history, presenter);

        await controller.ImportTextAsync("one two");

        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("History update failed.", controller.Status);
        Assert.Null(controller.LastSession);
        Assert.Empty(controller.History);
        Assert.Equal(0, presenter.PlayCount);
    }

    [Fact]
    public async Task ImportTextAsync_WhenPresenterFails_CancelsPlaybackClosesPresenterAndReturnsIdle()
    {
        var presenter = new FakeRsvpPresenter
        {
            PlayException = new InvalidOperationException("overlay failed")
        };
        var ticker = new FakeTicker();
        var controller = CreateController(
            AppSettings.CreateDefault(),
            new InMemoryHistoryStore(),
            presenter,
            player: new RsvpPlayer(ticker));

        await controller.ImportTextAsync("one two");

        Assert.Equal(AppState.Idle, controller.State);
        Assert.Equal("Playback failed.", controller.Status);
        Assert.Equal(PlayerState.Idle, presenter.LastPlayer!.State);
        Assert.False(ticker.IsRunning);
        Assert.Equal(1, presenter.CloseCount);
        Assert.NotNull(controller.LastSession);
    }

    [Fact]
    public async Task ImportTextAsync_WhenHistoryAddIsDelayed_DoesNotAllowReentry()
    {
        var history = new InMemoryHistoryStore { HoldAdd = true };
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), history, presenter);

        var firstImport = controller.ImportTextAsync("first text");
        await history.WaitForAddAsync();

        var secondImport = controller.ImportTextAsync("second text");

        history.ReleaseAdd();
        await Task.WhenAll(firstImport, secondImport);

        Assert.Equal(1, history.AddCount);
        Assert.Equal(["first text"], controller.History.Select(item => item.RawText));
        Assert.Equal("first text", controller.LastSession!.RawText);
        Assert.Equal(1, presenter.PlayCount);
    }

    [Fact]
    public async Task ReplayHistoryAsync_WhenStateChangedReentersBeforePlayback_DoesNotReplayTwice()
    {
        var presenter = new FakeRsvpPresenter();
        var firstItem = new HistoryItem(Guid.NewGuid(), DateTimeOffset.UtcNow, HistorySource.Import, "first text");
        var secondItem = new HistoryItem(Guid.NewGuid(), DateTimeOffset.UtcNow, HistorySource.Import, "second text");
        var controller = CreateController(
            AppSettings.CreateDefault(),
            new InMemoryHistoryStore(),
            presenter,
            loadedHistory: [firstItem, secondItem]);
        var reentered = false;
        controller.StateChanged += () =>
        {
            if (controller.State == AppState.Idle && !reentered)
            {
                reentered = true;
                _ = controller.ReplayHistoryAsync(secondItem);
            }
        };

        await controller.ReplayHistoryAsync(firstItem);

        Assert.Equal(1, presenter.PlayCount);
        Assert.Equal("first text", controller.LastSession!.RawText);
    }

    [Fact]
    public async Task ReplayLastAsync_WhenStateChangedReentersBeforePlayback_DoesNotReplayTwice()
    {
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), new InMemoryHistoryStore(), presenter);
        await controller.ImportTextAsync("first text");
        var reentered = false;
        controller.StateChanged += () =>
        {
            if (controller.State == AppState.Idle && !reentered)
            {
                reentered = true;
                _ = controller.ReplayLastAsync();
            }
        };

        await controller.ReplayLastAsync();

        Assert.Equal(2, presenter.PlayCount);
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

    [Fact]
    public void GetHotkeyBindings_ReturnsBindingsFromCurrentSettings()
    {
        var settings = AppSettings.CreateDefault() with
        {
            CaptureHotkey = new HotkeyBinding(ModifierKeys.Control, Key.D1),
            ReplayLastHotkey = new HotkeyBinding(ModifierKeys.Alt, Key.D2),
            PauseResumeHotkey = new HotkeyBinding(ModifierKeys.Shift, Key.D3),
            CancelHotkey = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Shift, Key.D4)
        };
        var controller = CreateController(settings, new InMemoryHistoryStore(), new FakeRsvpPresenter());

        var bindings = controller.GetHotkeyBindings();

        Assert.Equal(settings.CaptureHotkey, bindings[HotkeyAction.Capture]);
        Assert.Equal(settings.ReplayLastHotkey, bindings[HotkeyAction.ReplayLast]);
        Assert.Equal(settings.PauseResumeHotkey, bindings[HotkeyAction.PauseResume]);
        Assert.Equal(settings.CancelHotkey, bindings[HotkeyAction.Cancel]);
    }

    [Fact]
    public void SetHotkeyRegistrations_StoresRegistrationsAndTracksCaptureAvailability()
    {
        var settings = AppSettings.CreateDefault();
        var controller = CreateController(settings, new InMemoryHistoryStore(), new FakeRsvpPresenter());
        var notifications = 0;
        controller.StateChanged += () => notifications++;
        var registrations = new[]
        {
            new HotkeyRegistration(HotkeyAction.Capture, settings.CaptureHotkey, true),
            new HotkeyRegistration(HotkeyAction.ReplayLast, settings.ReplayLastHotkey, false)
        };

        controller.SetHotkeyRegistrations(registrations);

        Assert.True(controller.HotkeyAvailable);
        Assert.Equal(registrations, controller.HotkeyRegistrations);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task HandleHotkeyAsync_ReplaysLastSession()
    {
        var presenter = new FakeRsvpPresenter();
        var controller = CreateController(AppSettings.CreateDefault(), new InMemoryHistoryStore(), presenter);
        await controller.ImportTextAsync("one two");

        await controller.HandleHotkeyAsync(HotkeyAction.ReplayLast);

        Assert.Equal(2, presenter.PlayCount);
        Assert.Equal("one two", controller.LastSession!.RawText);
    }

    [Fact]
    public async Task HandleHotkeyAsync_AllowsPauseResumeAndCancelWhilePlaybackIsActive()
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

        await controller.HandleHotkeyAsync(HotkeyAction.PauseResume);
        Assert.Equal(PlayerState.Paused, presenter.LastPlayer!.State);

        await controller.HandleHotkeyAsync(HotkeyAction.Cancel);

        Assert.Equal(PlayerState.Idle, presenter.LastPlayer.State);
        Assert.Equal(AppState.Idle, controller.State);
        await playback;
    }

    [Fact]
    public async Task UpdateSettingsAsync_NormalizesSavesAppliesHistoryLimitAndReloadsHistory()
    {
        var initialSettings = AppSettings.CreateDefault() with { DefaultWpm = 300, HistoryLimit = 5 };
        var history = new InMemoryHistoryStore();
        await history.AddAsync("one", HistorySource.Import, 5);
        await history.AddAsync("two", HistorySource.Import, 5);
        var settingsStore = new InMemorySettingsStore(initialSettings);
        var controller = CreateController(
            initialSettings,
            history,
            new FakeRsvpPresenter(),
            settingsStore: settingsStore);

        var updated = initialSettings with { DefaultWpm = 999, HistoryLimit = 1 };
        await controller.UpdateSettingsAsync(updated);

        Assert.Equal(RsvpPlayer.MaximumWpm, controller.Settings.DefaultWpm);
        Assert.Equal(RsvpPlayer.MaximumWpm, controller.Wpm);
        Assert.Equal(1, controller.Settings.HistoryLimit);
        Assert.Equal(controller.Settings, settingsStore.Current);
        Assert.Equal(1, settingsStore.SaveCount);
        Assert.Equal(1, history.ApplyLimitCount);
        Assert.Equal(1, history.LastAppliedLimit);
        Assert.Single(controller.History);
        Assert.Equal("Ready.", controller.Status);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenSaveFailsKeepsCurrentSettingsAndReportsStatus()
    {
        var initialSettings = AppSettings.CreateDefault() with { DefaultWpm = 300, HistoryLimit = 5 };
        var settingsStore = new InMemorySettingsStore(initialSettings)
        {
            SaveException = new IOException("settings unavailable")
        };
        var history = new InMemoryHistoryStore();
        var controller = CreateController(
            initialSettings,
            history,
            new FakeRsvpPresenter(),
            settingsStore: settingsStore);

        await controller.UpdateSettingsAsync(initialSettings with { DefaultWpm = 400, HistoryLimit = 2 });

        Assert.Equal(initialSettings.Normalized(), controller.Settings);
        Assert.Equal(300, controller.Wpm);
        Assert.Equal("Settings save failed.", controller.Status);
        Assert.Equal(0, history.ApplyLimitCount);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenHistoryLimitFailsKeepsSavedSettingsAndReportsStatus()
    {
        var initialSettings = AppSettings.CreateDefault() with { HistoryLimit = 5 };
        var history = new InMemoryHistoryStore
        {
            ApplyLimitException = new IOException("history unavailable")
        };
        var settingsStore = new InMemorySettingsStore(initialSettings);
        var controller = CreateController(
            initialSettings,
            history,
            new FakeRsvpPresenter(),
            settingsStore: settingsStore);

        var updated = initialSettings with { HistoryLimit = 2 };
        await controller.UpdateSettingsAsync(updated);

        Assert.Equal(updated.Normalized(), controller.Settings);
        Assert.Equal(updated.Normalized(), settingsStore.Current);
        Assert.Equal("History limit update failed.", controller.Status);
    }

    private static AppController CreateController(
        AppSettings settings,
        InMemoryHistoryStore history,
        FakeRsvpPresenter presenter,
        IReadOnlyList<HistoryItem>? loadedHistory = null,
        IRegionSelector? selector = null,
        IScreenCaptureService? capture = null,
        IOcrService? ocr = null,
        RsvpPlayer? player = null,
        InMemorySettingsStore? settingsStore = null)
    {
        return new AppController(
            new FakeHotkeyService(),
            selector ?? new FakeRegionSelector(),
            capture ?? new FakeScreenCaptureService(),
            ocr,
            player ?? new RsvpPlayer(new FakeTicker()),
            presenter,
            settingsStore ?? new InMemorySettingsStore(settings),
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
        private TaskCompletionSource? beforePlayReached;
        private TaskCompletionSource? beforePlayRelease;
        private TaskCompletionSource? activePlayback;
        private bool holdBeforePlay;

        public bool HoldPlaybackOpen { get; init; }
        public bool HoldBeforePlay
        {
            get => holdBeforePlay;
            set
            {
                holdBeforePlay = value;
                if (value)
                {
                    beforePlayReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    beforePlayRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }
        public Exception? PlayException { get; init; }
        public int PlayCount { get; private set; }
        public int CloseCount { get; private set; }
        public RsvpPlayer? LastPlayer { get; private set; }
        public List<CaptureRegion?> Regions { get; } = [];

        public async Task PlayAsync(RsvpPlayer player, CaptureRegion? region)
        {
            if (HoldBeforePlay)
            {
                beforePlayReached!.SetResult();
                await beforePlayRelease!.Task;
                HoldBeforePlay = false;
            }

            PlayCount++;
            LastPlayer = player;
            Regions.Add(region);

            if (PlayException is not null)
            {
                throw PlayException;
            }

            player.Start();
            playbackStarted.TrySetResult();

            if (!HoldPlaybackOpen)
            {
                return;
            }

            activePlayback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await activePlayback.Task;
        }

        public Task WaitForPlaybackAsync()
        {
            return playbackStarted.Task;
        }

        public Task WaitForBeforePlayAsync()
        {
            return beforePlayReached?.Task ?? Task.CompletedTask;
        }

        public void ReleaseBeforePlay()
        {
            beforePlayRelease?.TrySetResult();
        }

        public void Close()
        {
            CloseCount++;
            activePlayback?.TrySetResult();
        }
    }
}
