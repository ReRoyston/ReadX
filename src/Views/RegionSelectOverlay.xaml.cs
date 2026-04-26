using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReadX.Models;

namespace ReadX.Views;

public partial class RegionSelectOverlay : Window
{
    private const int MinimumCaptureSize = 2;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;

    private readonly TaskCompletionSource<CaptureRegion?> selection =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private System.Windows.Point dragStartDip;
    private System.Windows.Point dragEndDip;
    private NativePoint dragStartPhysical;
    private bool isDragging;
    private bool isCompleted;

    public RegionSelectOverlay()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    public Task<CaptureRegion?> SelectAsync()
    {
        Show();
        Activate();
        Focus();
        return selection.Task;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetCursorPos(out dragStartPhysical))
        {
            Complete(null);
            return;
        }

        isDragging = true;
        dragStartDip = e.GetPosition(Surface);
        dragEndDip = dragStartDip;
        CaptureMouse();
        UpdateSelectionPreview();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!isDragging)
        {
            return;
        }

        dragEndDip = e.GetPosition(Surface);
        UpdateSelectionPreview();
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        ReleaseMouseCapture();

        if (!TryGetCursorPos(out var dragEndPhysical))
        {
            Complete(null);
            return;
        }

        var region = CreateRegion(dragStartPhysical, dragEndPhysical);
        Complete(region is { Width: >= MinimumCaptureSize, Height: >= MinimumCaptureSize } ? region : null);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Complete(null);
            e.Handled = true;
        }
    }

    private void UpdateSelectionPreview()
    {
        var left = Math.Min(dragStartDip.X, dragEndDip.X);
        var top = Math.Min(dragStartDip.Y, dragEndDip.Y);
        var width = Math.Abs(dragEndDip.X - dragStartDip.X);
        var height = Math.Abs(dragEndDip.Y - dragStartDip.Y);

        Canvas.SetLeft(SelectionRect, left);
        Canvas.SetTop(SelectionRect, top);
        SelectionRect.Width = width;
        SelectionRect.Height = height;
        SelectionRect.Visibility = Visibility.Visible;

        Canvas.SetLeft(Readout, left);
        Canvas.SetTop(Readout, Math.Max(0, top - 32));
        ReadoutText.Text = $"{Math.Round(width)} x {Math.Round(height)}  @ {Math.Round(left)}, {Math.Round(top)}";
        Readout.Visibility = Visibility.Visible;
    }

    private void Complete(CaptureRegion? region)
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;
        selection.TrySetResult(region);
        Close();
    }

    private static CaptureRegion CreateRegion(NativePoint start, NativePoint end)
    {
        var virtualScreenX = GetSystemMetrics(SmXVirtualScreen);
        var virtualScreenY = GetSystemMetrics(SmYVirtualScreen);

        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        return new CaptureRegion(left - virtualScreenX, top - virtualScreenY, width, height);
    }

    private static bool TryGetCursorPos(out NativePoint point)
    {
        return GetCursorPos(out point);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
