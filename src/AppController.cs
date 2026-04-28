using ReadX.Models;
using ReadX.Services;
using ReadX.Text;

namespace ReadX;

public sealed class AppController
{
    private readonly IHotkeyService hotkey;
    private readonly IRegionSelector selector;
    private readonly IScreenCaptureService capture;
    private readonly IOcrService? ocr;
    private readonly RsvpPlayer player;
    private readonly IRsvpPresenter presenter;
    private readonly ISettingsStore settingsStore;
    private readonly IHistoryStore history;

    public AppController(
        IHotkeyService hotkey,
        IRegionSelector selector,
        IScreenCaptureService capture,
        IOcrService? ocr,
        RsvpPlayer player,
        IRsvpPresenter presenter)
        : this(
            hotkey,
            selector,
            capture,
            ocr,
            player,
            presenter,
            new DefaultSettingsStore(),
            new EmptyHistoryStore(),
            AppSettings.CreateDefault(),
            [])
    {
    }

    public AppController(
        IHotkeyService hotkey,
        IRegionSelector selector,
        IScreenCaptureService capture,
        IOcrService? ocr,
        RsvpPlayer player,
        IRsvpPresenter presenter,
        ISettingsStore settingsStore,
        IHistoryStore history,
        AppSettings settings,
        IReadOnlyList<HistoryItem> loadedHistory)
    {
        this.hotkey = hotkey;
        this.selector = selector;
        this.capture = capture;
        this.ocr = ocr;
        this.player = player;
        this.presenter = presenter;
        this.settingsStore = settingsStore;
        this.history = history;

        Settings = settings.Normalized();
        History = loadedHistory.ToArray();
        Wpm = Settings.DefaultWpm;
    }

    public AppState State { get; private set; } = AppState.Idle;
    public string? Status { get; private set; }
    public RsvpSession? LastSession { get; private set; }
    public AppSettings Settings { get; private set; } = AppSettings.CreateDefault();
    public IReadOnlyList<HistoryItem> History { get; private set; } = [];
    public bool HotkeyAvailable { get; private set; }
    public bool OcrAvailable => ocr is not null;
    public int Wpm { get; set; } = 300;

    public event Action? StateChanged;

    public void SetHotkeyAvailable(bool available)
    {
        HotkeyAvailable = available;
        NotifyStateChanged();
    }

    public async Task StartCaptureAsync()
    {
        if (State != AppState.Idle || ocr is null)
        {
            return;
        }

        hotkey.SetBusy(true);

        try
        {
            SetState(AppState.Selecting, "Select a region.");
            var region = await selector.SelectAsync();
            if (region is null)
            {
                SetIdle(null);
                return;
            }

            SetState(AppState.Capturing, "Capturing.");
            using var image = Capture(region);
            if (image is null)
            {
                return;
            }

            SetState(AppState.Recognising, "Recognising text.");
            var text = Recognise(image);
            if (text is null)
            {
                return;
            }

            var session = await CreateSessionAsync(text, HistorySource.Capture, region, addToHistory: true);
            if (session is null)
            {
                return;
            }

            LastSession = session;
            NotifyStateChanged();

            await PlayAsync(session);
            SetIdle("Ready.");
        }
        finally
        {
            hotkey.SetBusy(false);
            if (State != AppState.Idle)
            {
                SetIdle(Status);
            }
        }
    }

    public async Task ReplayLastAsync()
    {
        if (State != AppState.Idle || LastSession is null)
        {
            return;
        }

        var session = LastSession;
        var replaySession = await CreateSessionAsync(session.RawText, session.Source, session.Region, addToHistory: false);
        if (replaySession is null)
        {
            return;
        }

        LastSession = replaySession;
        NotifyStateChanged();

        hotkey.SetBusy(true);

        try
        {
            await PlayAsync(replaySession);
            SetIdle("Ready.");
        }
        finally
        {
            hotkey.SetBusy(false);
            if (State != AppState.Idle)
            {
                SetIdle(Status);
            }
        }
    }

    public async Task ImportTextAsync(string rawText)
    {
        if (State != AppState.Idle)
        {
            return;
        }

        hotkey.SetBusy(true);

        try
        {
            var session = await CreateSessionAsync(rawText, HistorySource.Import, region: null, addToHistory: true);
            if (session is null)
            {
                return;
            }

            LastSession = session;
            NotifyStateChanged();

            await PlayAsync(session);
            SetIdle("Ready.");
        }
        finally
        {
            hotkey.SetBusy(false);
            if (State != AppState.Idle)
            {
                SetIdle(Status);
            }
        }
    }

    public async Task ReplayHistoryAsync(HistoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (State != AppState.Idle)
        {
            return;
        }

        hotkey.SetBusy(true);

        try
        {
            var session = await CreateSessionAsync(item.RawText, item.Source, region: null, addToHistory: false);
            if (session is null)
            {
                return;
            }

            LastSession = session;
            NotifyStateChanged();

            await PlayAsync(session);
            SetIdle("Ready.");
        }
        finally
        {
            hotkey.SetBusy(false);
            if (State != AppState.Idle)
            {
                SetIdle(Status);
            }
        }
    }

    public void PauseOrResumePlayback()
    {
        if (State == AppState.Playing)
        {
            player.Pause();
        }
    }

    public void RestartPlayback()
    {
        if (State == AppState.Playing)
        {
            player.Restart();
        }
    }

    public void CancelPlayback()
    {
        if (State != AppState.Playing)
        {
            return;
        }

        player.Cancel();
        presenter.Close();
        SetIdle("Ready.");
    }

    private System.Drawing.Bitmap? Capture(CaptureRegion region)
    {
        try
        {
            return capture.Capture(region);
        }
        catch
        {
            SetIdle("Capture failed.");
            return null;
        }
    }

    private string? Recognise(System.Drawing.Bitmap image)
    {
        try
        {
            var text = ocr!.Recognise(image);
            if (string.IsNullOrWhiteSpace(text))
            {
                SetIdle("No text recognised.");
                return null;
            }

            return text;
        }
        catch
        {
            SetIdle("OCR failed.");
            return null;
        }
    }

    private async Task<RsvpSession?> CreateSessionAsync(
        string rawText,
        HistorySource source,
        CaptureRegion? region,
        bool addToHistory)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            SetIdle("No text found.");
            return null;
        }

        if (addToHistory)
        {
            await history.AddAsync(rawText, source, Settings.HistoryLimit);
            History = await history.LoadAsync();
            NotifyStateChanged();
        }

        var pipeline = TextPipeline.BuildWords(rawText, Settings.CleanupEnabled);
        if (pipeline.Words.Count == 0)
        {
            SetIdle("No playable words.");
            return null;
        }

        return new RsvpSession(pipeline.Words, region, pipeline.RawText, pipeline.ProcessedText, source);
    }

    private async Task PlayAsync(RsvpSession session)
    {
        SetState(AppState.Playing, "Playing.");
        player.Load(session.Words, Wpm);
        await presenter.PlayAsync(player, session.Region);
    }

    private void SetState(AppState state, string? status)
    {
        State = state;
        Status = status;
        NotifyStateChanged();
    }

    private void SetIdle(string? status)
    {
        SetState(AppState.Idle, status);
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private sealed class DefaultSettingsStore : ISettingsStore
    {
        public Task<AppSettings> LoadAsync()
        {
            return Task.FromResult(AppSettings.CreateDefault());
        }

        public Task SaveAsync(AppSettings settings)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyHistoryStore : IHistoryStore
    {
        public Task<IReadOnlyList<HistoryItem>> LoadAsync()
        {
            return Task.FromResult<IReadOnlyList<HistoryItem>>([]);
        }

        public Task AddAsync(string rawText, HistorySource source, int limit)
        {
            return Task.CompletedTask;
        }

        public Task ApplyLimitAsync(int limit)
        {
            return Task.CompletedTask;
        }
    }
}
