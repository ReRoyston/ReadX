using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ReadX.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_WhenFileMissing_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(new TestPathProvider(root));

        var settings = await store.LoadAsync();

        Assert.Equal(300, settings.DefaultWpm);
        Assert.True(settings.CleanupEnabled);
        Assert.Equal(20, settings.HistoryLimit);
        Assert.Equal("Ctrl+Shift+R", settings.CaptureHotkey.ToDisplayText());
    }

    [Fact]
    public async Task LoadAsync_WhenJsonInvalid_ReturnsDefaults()
    {
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{ broken json");
        var store = new JsonSettingsStore(new TestPathProvider(root));

        var settings = await store.LoadAsync();

        Assert.Equal(AppSettings.CreateDefault(), settings);
    }

    [Fact]
    public async Task SaveAsync_WritesReadableJsonAndLoadAsyncReadsIt()
    {
        var store = new JsonSettingsStore(new TestPathProvider(root));
        var settings = AppSettings.CreateDefault() with
        {
            DefaultWpm = 450,
            CleanupEnabled = false,
            HistoryLimit = 7
        };

        await store.SaveAsync(settings);
        var reloaded = await store.LoadAsync();

        Assert.Equal(450, reloaded.DefaultWpm);
        Assert.False(reloaded.CleanupEnabled);
        Assert.Equal(7, reloaded.HistoryLimit);
    }

    [Fact]
    public async Task SaveAsync_WhenSuccessful_DoesNotLeaveTempFileAndWritesSettingsFile()
    {
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "settings.json.tmp"), "stale temp");
        var store = new JsonSettingsStore(new TestPathProvider(root));

        await store.SaveAsync(AppSettings.CreateDefault());

        Assert.True(File.Exists(Path.Combine(root, "settings.json")));
        Assert.False(File.Exists(Path.Combine(root, "settings.json.tmp")));
    }

    [Fact]
    public void Normalized_WhenWindowValuesAreNonFinite_UsesSafeFallbacks()
    {
        var settings = AppSettings.CreateDefault() with
        {
            WindowWidth = double.PositiveInfinity,
            WindowHeight = double.NegativeInfinity,
            WindowLeft = double.PositiveInfinity,
            WindowTop = double.NegativeInfinity
        };

        var normalized = settings.Normalized();

        Assert.Equal(860, normalized.WindowWidth);
        Assert.Equal(560, normalized.WindowHeight);
        Assert.True(double.IsNaN(normalized.WindowLeft));
        Assert.True(double.IsNaN(normalized.WindowTop));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestPathProvider(string path) : IAppDataPathProvider
    {
        public string ReadXDirectory => path;
    }
}
