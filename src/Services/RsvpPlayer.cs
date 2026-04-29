namespace ReadX.Services;

public enum PlayerState
{
    Idle,
    Playing,
    Paused,
    Finished
}

public sealed class RsvpPlayer
{
    public const int MinimumWpm = 100;
    public const int MaximumWpm = 800;

    private readonly ITicker ticker;
    private IReadOnlyList<string> words = [];

    public RsvpPlayer(ITicker ticker)
    {
        this.ticker = ticker;
        this.ticker.Tick += OnTick;
    }

    public PlayerState State { get; private set; } = PlayerState.Idle;
    public int Index { get; private set; } = -1;
    public int Total => words.Count;
    public string? CurrentWord => Index >= 0 && Index < words.Count ? words[Index] : null;

    public event Action<string>? WordChanged;
    public event Action? Completed;

    public void Load(IReadOnlyList<string> words, int wpm)
    {
        ArgumentNullException.ThrowIfNull(words);

        ticker.Stop();
        this.words = words.Where(word => !string.IsNullOrWhiteSpace(word)).ToArray();
        Index = -1;
        State = PlayerState.Idle;
        ticker.Interval = TimeSpan.FromMilliseconds(60000d / Math.Clamp(wpm, MinimumWpm, MaximumWpm));
    }

    public void Start()
    {
        if (State is PlayerState.Playing || words.Count == 0)
        {
            return;
        }

        State = PlayerState.Playing;
        ticker.Start();
    }

    public void Pause()
    {
        if (State == PlayerState.Playing)
        {
            State = PlayerState.Paused;
            ticker.Stop();
            return;
        }

        if (State == PlayerState.Paused)
        {
            State = PlayerState.Playing;
            ticker.Start();
        }
    }

    public void Restart()
    {
        if (words.Count == 0)
        {
            return;
        }

        ticker.Stop();
        Index = -1;
        State = PlayerState.Playing;
        ticker.Start();
    }

    public void Cancel()
    {
        ticker.Stop();
        Index = -1;
        State = PlayerState.Idle;
    }

    private void OnTick()
    {
        if (State != PlayerState.Playing)
        {
            return;
        }

        var nextIndex = Index + 1;
        if (nextIndex >= words.Count)
        {
            ticker.Stop();
            State = PlayerState.Finished;
            Completed?.Invoke();
            return;
        }

        Index = nextIndex;
        WordChanged?.Invoke(words[Index]);
    }
}
