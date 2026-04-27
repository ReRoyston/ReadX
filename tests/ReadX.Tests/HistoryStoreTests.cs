using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests;

public sealed class HistoryStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ReadX.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AddAsync_KeepsDuplicatesAndTrimsOldestByLimit()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));

        await store.AddAsync("same text", HistorySource.Import, limit: 2);
        await store.AddAsync("same text", HistorySource.Import, limit: 2);
        await store.AddAsync("new text", HistorySource.Capture, limit: 2);

        var items = await store.LoadAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(["new text", "same text"], items.Select(item => item.RawText));
        Assert.Equal([HistorySource.Capture, HistorySource.Import], items.Select(item => item.Source));
    }

    [Fact]
    public async Task AddAsync_DoesNotStoreWhitespaceOnlyText()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));

        await store.AddAsync(" \r\n ", HistorySource.Import, limit: 20);

        Assert.Empty(await store.LoadAsync());
    }

    [Fact]
    public async Task ApplyLimitAsync_TrimsExistingHistory()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));
        await store.AddAsync("one", HistorySource.Import, limit: 20);
        await store.AddAsync("two", HistorySource.Import, limit: 20);
        await store.AddAsync("three", HistorySource.Import, limit: 20);

        await store.ApplyLimitAsync(2);
        var items = await store.LoadAsync();

        Assert.Equal(["three", "two"], items.Select(item => item.RawText));
    }

    [Fact]
    public async Task AddAsync_WhenSuccessful_DoesNotLeaveTempFileAndWritesHistoryFile()
    {
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "history.json.tmp"), "stale temp");
        var store = new JsonHistoryStore(new TestPathProvider(root));

        await store.AddAsync("raw history", HistorySource.Import, limit: 20);

        Assert.True(File.Exists(Path.Combine(root, "history.json")));
        Assert.False(File.Exists(Path.Combine(root, "history.json.tmp")));
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
