using ReadX.Services;
using ReadX.Tests.Fakes;

namespace ReadX.Tests;

public class RsvpPlayerTests
{
    [Fact]
    public void LoadAndStart_AdvancesOneWordPerTick()
    {
        var ticker = new FakeTicker();
        var player = new RsvpPlayer(ticker);
        var seenWords = new List<string>();
        player.WordChanged += seenWords.Add;

        player.Load(["one", "two", "three"], 300);
        player.Start();
        ticker.Pulse();
        ticker.Pulse();

        Assert.Equal(PlayerState.Playing, player.State);
        Assert.Equal(1, player.Index);
        Assert.Equal("two", player.CurrentWord);
        Assert.Equal(["one", "two"], seenWords);
    }

    [Fact]
    public void Pause_HaltsTicksAndSecondPauseResumes()
    {
        var ticker = new FakeTicker();
        var player = new RsvpPlayer(ticker);
        var seenWords = new List<string>();
        player.WordChanged += seenWords.Add;

        player.Load(["one", "two"], 300);
        player.Start();
        player.Pause();
        ticker.Pulse();
        player.Pause();
        ticker.Pulse();

        Assert.Equal(PlayerState.Playing, player.State);
        Assert.Equal(["one"], seenWords);
    }

    [Fact]
    public void Restart_ReturnsToFirstWordAndStartsPlayback()
    {
        var ticker = new FakeTicker();
        var player = new RsvpPlayer(ticker);
        var seenWords = new List<string>();
        player.WordChanged += seenWords.Add;

        player.Load(["one", "two"], 300);
        player.Start();
        ticker.Pulse();
        ticker.Pulse();
        player.Restart();
        ticker.Pulse();

        Assert.Equal(PlayerState.Playing, player.State);
        Assert.Equal(0, player.Index);
        Assert.Equal(["one", "two", "one"], seenWords);
    }

    [Fact]
    public void EndOfWords_FiresCompletedAndFinishes()
    {
        var ticker = new FakeTicker();
        var player = new RsvpPlayer(ticker);
        var completedCount = 0;
        player.Completed += () => completedCount++;

        player.Load(["one"], 300);
        player.Start();
        ticker.Pulse();
        ticker.Pulse();

        Assert.Equal(PlayerState.Finished, player.State);
        Assert.False(ticker.IsRunning);
        Assert.Equal(1, completedCount);
    }

    [Theory]
    [InlineData(1, 600)]
    [InlineData(100, 600)]
    [InlineData(300, 200)]
    [InlineData(800, 75)]
    [InlineData(1000, 75)]
    public void Load_ClampsWpmAndSetsTickInterval(int wpm, double expectedMilliseconds)
    {
        var ticker = new FakeTicker();
        var player = new RsvpPlayer(ticker);

        player.Load(["one"], wpm);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), ticker.Interval);
    }
}
