using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemoryHistoryStore : IHistoryStore
{
    private readonly List<HistoryItem> items = [];
    private readonly List<(string RawText, HistorySource Source, int Limit)> adds = [];

    public int AddCount => adds.Count;
    public IReadOnlyList<(string RawText, HistorySource Source, int Limit)> Adds => adds.ToArray();

    public Task<IReadOnlyList<HistoryItem>> LoadAsync()
    {
        return Task.FromResult<IReadOnlyList<HistoryItem>>(items.ToArray());
    }

    public Task AddAsync(string rawText, HistorySource source, int limit)
    {
        adds.Add((rawText, source, limit));

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            items.Insert(0, new HistoryItem(Guid.NewGuid(), DateTimeOffset.Now, source, rawText));
            while (items.Count > limit)
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        return Task.CompletedTask;
    }

    public Task ApplyLimitAsync(int limit)
    {
        while (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
        }

        return Task.CompletedTask;
    }
}
