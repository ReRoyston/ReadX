using System.Windows.Threading;

namespace ReadX.Services;

public sealed class DispatcherTicker : ITicker
{
    private readonly DispatcherTimer timer;

    public DispatcherTicker()
    {
        timer = new DispatcherTimer();
        timer.Tick += (_, _) => Tick?.Invoke();
    }

    public TimeSpan Interval
    {
        get => timer.Interval;
        set => timer.Interval = value;
    }

    public event Action? Tick;

    public void Start()
    {
        timer.Start();
    }

    public void Stop()
    {
        timer.Stop();
    }
}
