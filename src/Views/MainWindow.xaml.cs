using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReadX.Models;
using ReadX.Services;
using MediaBrushes = System.Windows.Media.Brushes;

namespace ReadX.Views;

public partial class MainWindow : Window
{
    private bool isApplyingSettings;
    private bool isBusy;
    private bool ocrAvailable = true;
    private bool replayAvailable;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWpmText();
        UpdateHistoryLimitText(20);
        SetHotkeySummary(AppSettings.CreateDefault());
        ApplyControlState();
    }

    public event EventHandler? CaptureRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler? PauseResumeRequested;
    public event EventHandler? RestartRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler<string>? ImportRequested;
    public event EventHandler<HistoryItem>? HistoryReplayRequested;
    public event EventHandler? SettingsChangedByUser;
    public event EventHandler? WpmChanged;

    public int Wpm => (int)Math.Round(WpmSlider.Value);

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalized();
        isApplyingSettings = true;
        try
        {
            WpmSlider.Value = normalized.DefaultWpm;
            CleanupCheckBox.IsChecked = normalized.CleanupEnabled;
            HistoryLimitTextBox.Text = normalized.HistoryLimit.ToString(CultureInfo.InvariantCulture);
            CaptureHotkeyTextBox.Text = normalized.CaptureHotkey.ToDisplayText();
            ReplayHotkeyTextBox.Text = normalized.ReplayLastHotkey.ToDisplayText();
            PauseHotkeyTextBox.Text = normalized.PauseResumeHotkey.ToDisplayText();
            CancelHotkeyTextBox.Text = normalized.CancelHotkey.ToDisplayText();
            UpdateHistoryLimitText(normalized.HistoryLimit);
            SetHotkeySummary(normalized);

            if (IsSensibleSize(normalized.WindowWidth, MinWidth))
            {
                Width = normalized.WindowWidth;
            }

            if (IsSensibleSize(normalized.WindowHeight, MinHeight))
            {
                Height = normalized.WindowHeight;
            }

            if (double.IsFinite(normalized.WindowLeft))
            {
                Left = normalized.WindowLeft;
            }

            if (double.IsFinite(normalized.WindowTop))
            {
                Top = normalized.WindowTop;
            }

            WindowState = normalized.WindowMaximized ? WindowState.Maximized : WindowState.Normal;
        }
        finally
        {
            isApplyingSettings = false;
        }

        UpdateWpmText();
    }

    public AppSettings ReadSettings(AppSettings current)
    {
        ArgumentNullException.ThrowIfNull(current);

        var historyLimit = current.HistoryLimit;
        if (int.TryParse(HistoryLimitTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit)
            && parsedLimit > 0)
        {
            historyLimit = Math.Clamp(parsedLimit, 1, 500);
        }

        var bounds = WindowState != WindowState.Normal
            ? RestoreBounds
            : new Rect(Left, Top, Width, Height);

        return current with
        {
            DefaultWpm = Wpm,
            CleanupEnabled = CleanupCheckBox.IsChecked == true,
            HistoryLimit = historyLimit,
            CaptureHotkey = ReadHotkey(CaptureHotkeyTextBox.Text, current.CaptureHotkey),
            ReplayLastHotkey = ReadHotkey(ReplayHotkeyTextBox.Text, current.ReplayLastHotkey),
            PauseResumeHotkey = ReadHotkey(PauseHotkeyTextBox.Text, current.PauseResumeHotkey),
            CancelHotkey = ReadHotkey(CancelHotkeyTextBox.Text, current.CancelHotkey),
            WindowWidth = IsSensibleSize(bounds.Width, MinWidth) ? bounds.Width : current.WindowWidth,
            WindowHeight = IsSensibleSize(bounds.Height, MinHeight) ? bounds.Height : current.WindowHeight,
            WindowLeft = double.IsFinite(bounds.Left) ? bounds.Left : current.WindowLeft,
            WindowTop = double.IsFinite(bounds.Top) ? bounds.Top : current.WindowTop,
            WindowMaximized = WindowState == WindowState.Maximized
        };
    }

    public void SetHistory(IReadOnlyList<HistoryItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        HistoryList.Items.Clear();

        foreach (var item in items)
        {
            HistoryList.Items.Add(CreateHistoryItemView(item));
        }

        if (items.Count == 0)
        {
            HistoryList.Items.Add(new TextBlock
            {
                Text = "No history yet.",
                Foreground = MediaBrushes.LightSlateGray,
                Padding = new Thickness(10)
            });
        }
    }

    public void SetHotkeyStatus(IReadOnlyList<HotkeyRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        HotkeySummaryText.Text = string.Join(
            Environment.NewLine,
            registrations.Select(registration =>
                $"{FormatHotkeyAction(registration.Action)}: {registration.Binding.ToDisplayText()} {(registration.IsRegistered ? "registered" : "unavailable")}"));
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;
        ApplyControlState();
    }

    public void SetHotkeyAvailable(bool available)
    {
        CaptureHotkeyText.Text = available ? "Hotkey ready" : "Hotkey unavailable";
        CaptureHotkeyText.Foreground = available ? MediaBrushes.LightGreen : MediaBrushes.IndianRed;
    }

    public void SetOcrAvailable(bool available)
    {
        ocrAvailable = available;
        OcrStatusText.Text = available ? "OCR ready" : "OCR unavailable";
        OcrStatusText.Foreground = available ? MediaBrushes.LightGreen : MediaBrushes.IndianRed;
        ApplyControlState();
    }

    public void SetReplayAvailable(bool available)
    {
        replayAvailable = available;
        ApplyControlState();
    }

    public void SetLastOcrText(string? text)
    {
        LastTextBox.Text = string.IsNullOrWhiteSpace(text) ? "No text yet." : text;
    }

    public void SetStatus(string? status)
    {
        StatusText.Text = string.IsNullOrWhiteSpace(status) ? "Ready." : status;
    }

    private static bool IsSensibleSize(double value, double minimum)
    {
        return double.IsFinite(value) && value >= minimum;
    }

    private static string FormatHotkeyAction(HotkeyAction action)
    {
        return action switch
        {
            HotkeyAction.Capture => "Capture",
            HotkeyAction.ReplayLast => "Replay last",
            HotkeyAction.PauseResume => "Pause/resume",
            HotkeyAction.Cancel => "Cancel",
            _ => action.ToString()
        };
    }

    private static string PreviewText(string text)
    {
        var compact = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 110 ? compact : compact[..107] + "...";
    }

    private static HotkeyBinding ReadHotkey(string text, HotkeyBinding fallback)
    {
        return HotkeyBinding.TryParseDisplayText(text, out var binding) ? binding : fallback;
    }

    private FrameworkElement CreateHistoryItemView(HistoryItem item)
    {
        var root = new Grid
        {
            Margin = new Thickness(0, 0, 0, 6)
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Margin = new Thickness(8, 6, 10, 6) };
        textStack.Children.Add(new TextBlock
        {
            Text = $"{item.CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} - {item.Source}",
            Foreground = MediaBrushes.LightSlateGray,
            FontSize = 12
        });
        textStack.Children.Add(new TextBlock
        {
            Text = PreviewText(item.RawText),
            TextWrapping = TextWrapping.Wrap,
            Foreground = MediaBrushes.Gainsboro,
            Margin = new Thickness(0, 3, 0, 0)
        });

        var replayButton = new Wpf.Ui.Controls.Button
        {
            Width = 72,
            Height = 30,
            Margin = new Thickness(0, 8, 8, 8),
            Content = "Replay",
            Tag = item,
            IsEnabled = !isBusy
        };
        replayButton.Click += HistoryReplayButton_Click;

        Grid.SetColumn(textStack, 0);
        Grid.SetColumn(replayButton, 1);
        root.Children.Add(textStack);
        root.Children.Add(replayButton);

        return root;
    }

    private void SetHotkeySummary(AppSettings settings)
    {
        HotkeySummaryText.Text = string.Join(
            Environment.NewLine,
            [
                $"Capture: {settings.CaptureHotkey.ToDisplayText()} configured",
                $"Replay last: {settings.ReplayLastHotkey.ToDisplayText()} configured",
                $"Pause/resume: {settings.PauseResumeHotkey.ToDisplayText()} configured",
                $"Cancel: {settings.CancelHotkey.ToDisplayText()} configured"
            ]);
    }

    private void UpdateHistoryLimitText(int historyLimit)
    {
        HistoryLimitText.Text = $"History limit: {historyLimit}";
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReplayButton_Click(object sender, RoutedEventArgs e)
    {
        ReplayRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        PauseResumeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        RestartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PlayImportButton_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested?.Invoke(this, ImportTextBox.Text);
    }

    private void ClearImportButton_Click(object sender, RoutedEventArgs e)
    {
        ImportTextBox.Clear();
    }

    private void HistoryReplayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: HistoryItem item })
        {
            HistoryReplayRequested?.Invoke(this, item);
        }
    }

    private void TabButton_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        CapturePanel.Visibility = CaptureTabButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ImportPanel.Visibility = ImportTabButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        HistoryPanel.Visibility = HistoryTabButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = SettingsTabButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void WpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized)
        {
            return;
        }

        UpdateWpmText();
        if (isApplyingSettings)
        {
            return;
        }

        WpmChanged?.Invoke(this, EventArgs.Empty);
        SettingsChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        RaiseSettingsChangedByUser();
    }

    private void SettingsTextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(HistoryLimitTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit)
            && parsedLimit > 0)
        {
            UpdateHistoryLimitText(Math.Clamp(parsedLimit, 1, 500));
        }

        RaiseSettingsChangedByUser();
    }

    private void RaiseSettingsChangedByUser()
    {
        if (!IsInitialized || isApplyingSettings)
        {
            return;
        }

        SettingsChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateWpmText()
    {
        WpmValueText.Text = Wpm.ToString(CultureInfo.InvariantCulture);
    }

    private void ApplyControlState()
    {
        CaptureButton.IsEnabled = ocrAvailable && !isBusy;
        ReplayButton.IsEnabled = replayAvailable && !isBusy;
        PlayImportButton.IsEnabled = !isBusy;
        WpmSlider.IsEnabled = !isBusy;
        PauseResumeButton.IsEnabled = isBusy;
        RestartButton.IsEnabled = isBusy;
        CancelButton.IsEnabled = isBusy;

        foreach (var button in HistoryList.Items
                     .OfType<DependencyObject>()
                     .SelectMany(FindDescendantButtons)
                     .Where(button => button.Tag is HistoryItem))
        {
            button.IsEnabled = !isBusy;
        }
    }

    private static IEnumerable<System.Windows.Controls.Button> FindDescendantButtons(DependencyObject root)
    {
        if (root is System.Windows.Controls.Button button)
        {
            yield return button;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            foreach (var descendant in FindDescendantButtons(child))
            {
                yield return descendant;
            }
        }
    }
}
