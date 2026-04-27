using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class FakeTicker : ITicker
{
    public TimeSpan Interval { get; set; }
    public bool IsRunning { get; private set; }

    public event Action? Tick;

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Pulse()
    {
        if (IsRunning)
        {
            Tick?.Invoke();
        }
    }
}
