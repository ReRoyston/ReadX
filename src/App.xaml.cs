using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
    private ISettingsStore? settingsStore;
    private IHistoryStore? historyStore;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _ = StartAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        hotkey?.Dispose();
        ocr?.Dispose();
        presenter?.Close();
        SaveFinalSettings();
        base.OnExit(e);
    }

    private async Task StartAsync()
    {
        try
        {
            var pathProvider = new AppDataPathProvider();
            settingsStore = new JsonSettingsStore(pathProvider);
            historyStore = new JsonHistoryStore(pathProvider);
            var settings = await settingsStore.LoadAsync();
            var historyItems = await historyStore.LoadAsync();

            ComposeApplication(settings, historyItems);
        }
        catch
        {
            System.Windows.MessageBox.Show(
                "ReadX could not start because persisted app data could not be loaded.",
                "ReadX",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ComposeApplication(AppSettings settings, IReadOnlyList<HistoryItem> historyItems)
    {
        mainWindow = new MainWindow();
        mainWindow.ApplySettings(settings);
        mainWindow.SetHistory(historyItems);

        hotkey = new HotkeyService();
        presenter = new RsvpPresenter();
        ocr = TryCreateOcrService();

        controller = new AppController(
            hotkey,
            new RegionSelector(),
            new ScreenCaptureService(),
            ocr,
            new RsvpPlayer(new DispatcherTicker()),
            presenter,
            settingsStore!,
            historyStore!,
            settings,
            historyItems);

        controller.StateChanged += ApplyControllerState;
        WireMainWindowEvents();
        mainWindow.SourceInitialized += (_, _) => RegisterHotkeys();

        ApplyControllerState();
        mainWindow.Show();
    }

    private void WireMainWindowEvents()
    {
        if (mainWindow is null || controller is null)
        {
            return;
        }

        mainWindow.CaptureRequested += (_, _) => _ = controller.StartCaptureAsync();
        mainWindow.ImportRequested += (_, text) => _ = controller.ImportTextAsync(text);
        mainWindow.ReplayRequested += (_, _) => _ = controller.ReplayLastAsync();
        mainWindow.HistoryReplayRequested += (_, item) => _ = controller.ReplayHistoryAsync(item);
        mainWindow.PauseResumeRequested += (_, _) => controller.PauseOrResumePlayback();
        mainWindow.RestartRequested += (_, _) => controller.RestartPlayback();
        mainWindow.CancelRequested += (_, _) => controller.CancelPlayback();
        mainWindow.WpmChanged += (_, _) => controller.Wpm = mainWindow.Wpm;
        mainWindow.SettingsChangedByUser += (_, _) => _ = UpdateSettingsFromMainWindowAsync();
    }

    private void RegisterHotkeys()
    {
        if (mainWindow is null || controller is null || hotkey is null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(mainWindow).Handle;
        var bindings = controller.GetHotkeyBindings();
        IReadOnlyList<HotkeyRegistration> registrations;
        try
        {
            registrations = hotkey.RegisterAll(
                hwnd,
                bindings,
                action => _ = controller.HandleHotkeyAsync(action));
        }
        catch
        {
            registrations = bindings
                .Select(pair => new HotkeyRegistration(pair.Key, pair.Value, false))
                .ToArray();
        }

        controller.SetHotkeyRegistrations(registrations);
    }

    private async Task UpdateSettingsFromMainWindowAsync()
    {
        if (mainWindow is null || controller is null)
        {
            return;
        }

        try
        {
            var updated = mainWindow.ReadSettings(controller.Settings);
            await controller.UpdateSettingsAsync(updated);
            RegisterHotkeys();
        }
        catch
        {
            ApplyControllerState();
        }
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
        mainWindow.SetHistory(controller.History);

        if (controller.HotkeyRegistrations.Count > 0)
        {
            mainWindow.SetHotkeyStatus(controller.HotkeyRegistrations);
        }
    }

    private void SaveFinalSettings()
    {
        if (mainWindow is null || controller is null || settingsStore is null)
        {
            return;
        }

        try
        {
            var finalSettings = mainWindow.ReadSettings(controller.Settings);
            settingsStore.SaveAsync(finalSettings).GetAwaiter().GetResult();
        }
        catch
        {
            // Exit cleanup should still release OS resources even if settings cannot be saved.
        }
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
