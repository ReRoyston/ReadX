namespace ReadX.Services;

public interface ITicker
{
    TimeSpan Interval { get; set; }
    event Action? Tick;
    void Start();
    void Stop();
}
