using System.Windows;
using ReadX.Models;
using ReadX.Views;

namespace ReadX.Services;

public sealed class RsvpPresenter : IRsvpPresenter
{
    private RsvpOverlay? overlay;
    private TaskCompletionSource? activePlayback;

    public Task PlayAsync(RsvpPlayer player, CaptureRegion region)
    {
        ArgumentNullException.ThrowIfNull(player);

        Close();

        if (player.Total == 0)
        {
            return Task.CompletedTask;
        }

        activePlayback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var playbackOverlay = new RsvpOverlay();
        overlay = playbackOverlay;
        PositionOverlay(playbackOverlay, region);

        var detached = false;

        void OnWordChanged(string word)
        {
            playbackOverlay.ShowWord(word, player.Index + 1, player.Total);
        }

        void OnPauseRequested()
        {
            player.Pause();
        }

        void OnRestartRequested()
        {
            player.Restart();
        }

        void DetachHandlers()
        {
            if (detached)
            {
                return;
            }

            detached = true;
            player.WordChanged -= OnWordChanged;
            player.Completed -= OnCompleted;
            playbackOverlay.PauseRequested -= OnPauseRequested;
            playbackOverlay.RestartRequested -= OnRestartRequested;
            playbackOverlay.CancelRequested -= OnCancelRequested;
        }

        void OnCompleted()
        {
            DetachHandlers();
            Close();
        }

        void OnCancelRequested()
        {
            player.Cancel();
            DetachHandlers();
            Close();
        }

        playbackOverlay.PauseRequested += OnPauseRequested;
        playbackOverlay.RestartRequested += OnRestartRequested;
        playbackOverlay.CancelRequested += OnCancelRequested;
        playbackOverlay.Closed += (_, _) =>
        {
            DetachHandlers();
            activePlayback?.TrySetResult();
            activePlayback = null;
            overlay = null;
        };

        player.WordChanged += OnWordChanged;
        player.Completed += OnCompleted;

        playbackOverlay.Show();
        playbackOverlay.Activate();
        player.Start();

        return activePlayback.Task;
    }

    public void Close()
    {
        if (overlay is null)
        {
            activePlayback?.TrySetResult();
            activePlayback = null;
            return;
        }

        var closing = overlay;
        overlay = null;
        closing.Close();
    }

    private static void PositionOverlay(Window window, CaptureRegion region)
    {
        const double margin = 12;

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;
        var absoluteRegionLeft = virtualLeft + region.X;
        var absoluteRegionTop = virtualTop + region.Y;

        var left = absoluteRegionLeft + (region.Width - window.Width) / 2d;
        var top = absoluteRegionTop - window.Height - margin;

        left = Math.Clamp(left, virtualLeft, Math.Max(virtualLeft, virtualRight - window.Width));

        if (top < virtualTop)
        {
            top = absoluteRegionTop + region.Height + margin;
        }

        if (top + window.Height > virtualBottom)
        {
            left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - window.Width) / 2d;
            top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - window.Height) / 2d;
        }

        window.Left = left;
        window.Top = top;
    }
}
