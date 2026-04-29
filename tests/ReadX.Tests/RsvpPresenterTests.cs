using ReadX.Models;
using ReadX.Services;
using ReadX.Tests.Fakes;

namespace ReadX.Tests;

public sealed class RsvpPresenterTests
{
    [Fact]
    public void Close_WhenPlaybackIsActive_CancelsPlayer()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var ticker = new FakeTicker();
                var player = new RsvpPlayer(ticker);
                var presenter = new RsvpPresenter();

                player.Load(["one", "two"], 300);
                var playback = presenter.PlayAsync(player, new CaptureRegion(0, 0, 100, 100));

                Assert.Equal(PlayerState.Playing, player.State);
                Assert.False(playback.IsCompleted);

                presenter.Close();

                Assert.True(playback.IsCompleted);
                Assert.Equal(PlayerState.Idle, player.State);
                Assert.False(ticker.IsRunning);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void CompletedPlayback_WhenOverlayCloses_KeepsPlayerFinished()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var ticker = new FakeTicker();
                var player = new RsvpPlayer(ticker);
                var presenter = new RsvpPresenter();

                player.Load(["one"], 300);
                var playback = presenter.PlayAsync(player, new CaptureRegion(0, 0, 100, 100));

                ticker.Pulse();
                ticker.Pulse();

                Assert.True(playback.IsCompleted);
                Assert.Equal(PlayerState.Finished, player.State);
                Assert.False(ticker.IsRunning);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void PlayAsync_WhenRegionIsNull_StartsPlaybackWithDefaultOverlayPosition()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var ticker = new FakeTicker();
                var player = new RsvpPlayer(ticker);
                var presenter = new RsvpPresenter();

                player.Load(["one", "two"], 300);
                var playback = presenter.PlayAsync(player, null);

                Assert.Equal(PlayerState.Playing, player.State);
                Assert.False(playback.IsCompleted);

                presenter.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }
}
