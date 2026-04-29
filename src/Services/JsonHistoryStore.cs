using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using ReadX.Models;

namespace ReadX.Services;

public sealed class JsonHistoryStore(IAppDataPathProvider pathProvider) : IHistoryStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string historyPath = Path.Combine(pathProvider.ReadXDirectory, "history.json");
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<IReadOnlyList<HistoryItem>> LoadAsync()
    {
        await gate.WaitAsync();
        try
        {
            return await LoadCoreAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddAsync(string rawText, HistorySource source, int limit)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return;
        }

        await gate.WaitAsync();
        try
        {
            var items = (await LoadCoreAsync()).ToList();
            items.Insert(0, new HistoryItem(Guid.NewGuid(), DateTimeOffset.Now, source, rawText));
            await SaveAsync(Trim(items, limit));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ApplyLimitAsync(int limit)
    {
        await gate.WaitAsync();
        try
        {
            var items = await LoadCoreAsync();
            await SaveAsync(Trim(items, limit));
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<IReadOnlyList<HistoryItem>> LoadCoreAsync()
    {
        if (Directory.Exists(historyPath))
        {
            throw new IOException($"History path is a directory: {historyPath}");
        }

        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(historyPath);
            var items = await JsonSerializer.DeserializeAsync<List<HistoryItem>>(stream, Options);
            return (items ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.RawText))
                .OrderByDescending(item => item.CreatedAt)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task SaveAsync(IReadOnlyList<HistoryItem> items)
    {
        var directory = Path.GetDirectoryName(historyPath)!;
        var tempPath = Path.Combine(directory, $"history.json.{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(directory);

        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, items, Options);
            }

            File.Move(tempPath, historyPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static IReadOnlyList<HistoryItem> Trim(IEnumerable<HistoryItem> items, int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 500);
        return items
            .OrderByDescending(item => item.CreatedAt)
            .Take(normalizedLimit)
            .ToArray();
    }
}
