using ReadX.Models;
using ReadX.Services;
using ReadX.Tokenization;

namespace ReadX;

public sealed class AppController
{
    private readonly IHotkeyService hotkey;
    private readonly IRegionSelector selector;
    private readonly IScreenCaptureService capture;
    private readonly IOcrService? ocr;
    private readonly RsvpPlayer player;
    private readonly IRsvpPresenter presenter;

    public AppController(
        IHotkeyService hotkey,
        IRegionSelector selector,
        IScreenCaptureService capture,
        IOcrService? ocr,
        RsvpPlayer player,
        IRsvpPresenter presenter)
    {
        this.hotkey = hotkey;
        this.selector = selector;
        this.capture = capture;
        this.ocr = ocr;
        this.player = player;
        this.presenter = presenter;
    }

    public AppState State { get; private set; } = AppState.Idle;
    public string? Status { get; private set; }
    public RsvpSession? LastSession { get; private set; }
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

            var words = WordSplitter.Split(text);
            if (words.Count == 0)
            {
                SetIdle("No text recognised.");
                return;
            }

            LastSession = new RsvpSession(words, region, text);
            NotifyStateChanged();

            await PlayAsync(words, region);
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
        var region = session.Region;
        if (region is null)
        {
            SetIdle("No capture region available for replay.");
            return;
        }

        hotkey.SetBusy(true);

        try
        {
            await PlayAsync(session.Words, region);
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

    private async Task PlayAsync(IReadOnlyList<string> words, CaptureRegion region)
    {
        SetState(AppState.Playing, "Playing.");
        player.Load(words, Wpm);
        await presenter.PlayAsync(player, region);
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
}
