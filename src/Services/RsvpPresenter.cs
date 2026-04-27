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
        overlay = new RsvpOverlay();
        PositionOverlay(overlay, region);

        void OnWordChanged(string word)
        {
            overlay?.ShowWord(word, player.Index + 1, player.Total);
        }

        void OnCompleted()
        {
            player.WordChanged -= OnWordChanged;
            player.Completed -= OnCompleted;
            Close();
        }

        overlay.PauseRequested += player.Pause;
        overlay.CancelRequested += () =>
        {
            player.Cancel();
            player.WordChanged -= OnWordChanged;
            player.Completed -= OnCompleted;
            Close();
        };
        overlay.Closed += (_, _) =>
        {
            player.WordChanged -= OnWordChanged;
            player.Completed -= OnCompleted;
            activePlayback?.TrySetResult();
            activePlayback = null;
            overlay = null;
        };

        player.WordChanged += OnWordChanged;
        player.Completed += OnCompleted;

        overlay.Show();
        overlay.Activate();
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
