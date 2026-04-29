using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReadX.Models;
using ReadX.Services;
using ReadX.Views;

namespace ReadX.Tests;

public sealed class MainWindowTests
{
    [Fact]
    public void ApplySettingsDoesNotRaiseUserChangeEventsAndReadSettingsPreservesInvalidLimit()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var userChanges = 0;
                var wpmChanges = 0;
                window.SettingsChangedByUser += (_, _) => userChanges++;
                window.WpmChanged += (_, _) => wpmChanges++;

                window.ApplySettings(AppSettings.CreateDefault() with
                {
                    DefaultWpm = 450,
                    CleanupEnabled = false,
                    HistoryLimit = 12,
                    WindowWidth = 900,
                    WindowHeight = 620,
                    WindowLeft = 32,
                    WindowTop = 48
                });

                Assert.Equal(0, userChanges);
                Assert.Equal(0, wpmChanges);
                Assert.Equal(450, window.Wpm);

                var historyLimitBox = (TextBox)window.FindName("HistoryLimitTextBox");
                historyLimitBox.Text = "not a number";

                var read = window.ReadSettings(AppSettings.CreateDefault() with
                {
                    HistoryLimit = 77
                });

                Assert.Equal(450, read.DefaultWpm);
                Assert.False(read.CleanupEnabled);
                Assert.Equal(77, read.HistoryLimit);
                Assert.Equal(900, read.WindowWidth);
                Assert.Equal(620, read.WindowHeight);
                Assert.Equal(32, read.WindowLeft);
                Assert.Equal(48, read.WindowTop);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReadSettingsUsesRestoreBoundsWhenWindowIsMinimized()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            try
            {
                window.ApplySettings(AppSettings.CreateDefault() with
                {
                    WindowWidth = 900,
                    WindowHeight = 620,
                    WindowLeft = 32,
                    WindowTop = 48
                });
                window.Show();

                window.WindowState = WindowState.Minimized;

                var read = window.ReadSettings(AppSettings.CreateDefault());

                Assert.Equal(900, read.WindowWidth);
                Assert.Equal(620, read.WindowHeight);
                Assert.Equal(32, read.WindowLeft);
                Assert.Equal(48, read.WindowTop);
                Assert.False(read.WindowMaximized);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetHistoryCreatesReplayButtonsTaggedWithHistoryItems()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var item = new HistoryItem(Guid.NewGuid(), DateTimeOffset.Parse("2026-04-29T10:15:00+10:00"), HistorySource.Import, "This is a long imported text preview that should be shortened.");
                HistoryItem? replayed = null;
                window.HistoryReplayRequested += (_, historyItem) => replayed = historyItem;

                window.SetHistory([item]);

                var list = (ListBox)window.FindName("HistoryList");
                Assert.Single(list.Items);
                var replayButton = FindDescendant<Button>((DependencyObject)list.Items[0], button => Equals(button.Tag, item));

                Assert.NotNull(replayButton);
                replayButton!.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                Assert.Equal(item, replayed);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetHotkeyStatusSummarizesEachRegistration()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            try
            {
                window.SetHotkeyStatus(
                [
                    new HotkeyRegistration(HotkeyAction.Capture, AppSettings.CreateDefault().CaptureHotkey, true),
                    new HotkeyRegistration(HotkeyAction.ReplayLast, AppSettings.CreateDefault().ReplayLastHotkey, false)
                ]);

                var summary = (TextBlock)window.FindName("HotkeySummaryText");

                Assert.Contains("Capture: Ctrl+Shift+R registered", summary.Text);
                Assert.Contains("Replay last: Ctrl+Shift+E unavailable", summary.Text);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReadSettingsReadsEditedHotkeyBindingsAndKeepsFallbackForInvalidBinding()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var current = AppSettings.CreateDefault();
                window.ApplySettings(current);

                ((TextBox)window.FindName("CaptureHotkeyTextBox")).Text = "Ctrl+Alt+D1";
                ((TextBox)window.FindName("ReplayHotkeyTextBox")).Text = "Shift+D2";
                ((TextBox)window.FindName("PauseHotkeyTextBox")).Text = "DefinitelyNotAKey";
                ((TextBox)window.FindName("CancelHotkeyTextBox")).Text = "Ctrl+Esc";

                var read = window.ReadSettings(current);

                Assert.Equal(new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Alt, Key.D1), read.CaptureHotkey);
                Assert.Equal(new HotkeyBinding(ModifierKeys.Shift, Key.D2), read.ReplayLastHotkey);
                Assert.Equal(current.PauseResumeHotkey, read.PauseResumeHotkey);
                Assert.Equal(new HotkeyBinding(ModifierKeys.Control, Key.Escape), read.CancelHotkey);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate)
        where T : DependencyObject
    {
        if (root is T candidate && predicate(candidate))
        {
            return candidate;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            var found = FindDescendant(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
