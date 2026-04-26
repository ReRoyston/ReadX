using System;
using System.Windows;

namespace ReadX.Views;

public partial class MainWindow : Window
{
    private bool isBusy;
    private bool ocrAvailable = true;
    private bool replayAvailable;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWpmText();
        ApplyControlState();
    }

    public event EventHandler? CaptureRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler? WpmChanged;

    public int Wpm => (int)Math.Round(WpmSlider.Value);

    public void SetBusy(bool busy)
    {
        isBusy = busy;
        ApplyControlState();
    }

    public void SetHotkeyAvailable(bool available)
    {
        HotkeyStatusText.Text = available ? "Ready" : "Unavailable";
        HotkeyStatusText.Foreground = available ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.IndianRed;
    }

    public void SetOcrAvailable(bool available)
    {
        ocrAvailable = available;
        OcrStatusText.Text = available ? "Ready" : "Unavailable";
        OcrStatusText.Foreground = available ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.IndianRed;
        ApplyControlState();
    }

    public void SetReplayAvailable(bool available)
    {
        replayAvailable = available;
        ApplyControlState();
    }

    public void SetLastOcrText(string? text)
    {
        LastOcrTextBox.Text = string.IsNullOrWhiteSpace(text) ? "No capture yet." : text;
    }

    public void SetStatus(string? status)
    {
        StatusText.Text = string.IsNullOrWhiteSpace(status) ? "Ready." : status;
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReplayButton_Click(object sender, RoutedEventArgs e)
    {
        ReplayRequested?.Invoke(this, EventArgs.Empty);
    }

    private void WpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized)
        {
            return;
        }

        UpdateWpmText();
        WpmChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateWpmText()
    {
        WpmValueText.Text = Wpm.ToString();
    }

    private void ApplyControlState()
    {
        CaptureButton.IsEnabled = ocrAvailable && !isBusy;
        ReplayButton.IsEnabled = replayAvailable && !isBusy;
        WpmSlider.IsEnabled = !isBusy;
    }
}
