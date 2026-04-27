using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ReadX.Models;
using ReadX.Services;
using ReadX.Views;

namespace ReadX;

public partial class App : System.Windows.Application
{
    private MainWindow? mainWindow;
    private AppController? controller;
    private IHotkeyService? hotkey;
    private IOcrService? ocr;
    private IRsvpPresenter? presenter;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        mainWindow = new MainWindow();
        hotkey = new HotkeyService();
        presenter = new RsvpPresenter();
        ocr = TryCreateOcrService();

        controller = new AppController(
            hotkey,
            new RegionSelector(),
            new ScreenCaptureService(),
            ocr,
            new RsvpPlayer(new DispatcherTicker()),
            presenter)
        {
            Wpm = mainWindow.Wpm
        };

        controller.StateChanged += ApplyControllerState;
        mainWindow.CaptureRequested += (_, _) => _ = controller.StartCaptureAsync();
        mainWindow.ReplayRequested += (_, _) => _ = controller.ReplayLastAsync();
        mainWindow.WpmChanged += (_, _) => controller.Wpm = mainWindow.Wpm;
        mainWindow.SourceInitialized += MainWindow_SourceInitialized;

        ApplyControllerState();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        hotkey?.Dispose();
        ocr?.Dispose();
        presenter?.Close();
        base.OnExit(e);
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (mainWindow is null || controller is null || hotkey is null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(mainWindow).Handle;
        var registered = hotkey.TryRegister(
            hwnd,
            ModifierKeys.Control | ModifierKeys.Shift,
            Key.R,
            () => _ = controller.StartCaptureAsync());

        controller.SetHotkeyAvailable(registered);
    }

    private void ApplyControllerState()
    {
        if (mainWindow is null || controller is null)
        {
            return;
        }

        mainWindow.SetBusy(controller.State != AppState.Idle);
        mainWindow.SetHotkeyAvailable(controller.HotkeyAvailable);
        mainWindow.SetOcrAvailable(controller.OcrAvailable);
        mainWindow.SetReplayAvailable(controller.LastSession is not null);
        mainWindow.SetLastOcrText(controller.LastSession?.Text);
        mainWindow.SetStatus(controller.Status);
    }

    private static IOcrService? TryCreateOcrService()
    {
        try
        {
            return new OcrService(Path.Combine(AppContext.BaseDirectory, "tessdata"));
        }
        catch
        {
            return null;
        }
    }
}
