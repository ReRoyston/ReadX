using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemoryHistoryStore : IHistoryStore
{
    private readonly List<HistoryItem> items = [];
    private readonly List<(string RawText, HistorySource Source, int Limit)> adds = [];
    private readonly TaskCompletionSource addReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource addRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int AddCount => adds.Count;
    public IReadOnlyList<(string RawText, HistorySource Source, int Limit)> Adds => adds.ToArray();
    public Exception? AddException { get; init; }
    public Exception? LoadException { get; init; }
    public bool HoldAdd { get; init; }

    public Task<IReadOnlyList<HistoryItem>> LoadAsync()
    {
        if (LoadException is not null)
        {
            throw LoadException;
        }

        return Task.FromResult<IReadOnlyList<HistoryItem>>(items.ToArray());
    }

    public async Task AddAsync(string rawText, HistorySource source, int limit)
    {
        if (HoldAdd)
        {
            addReached.TrySetResult();
            await addRelease.Task;
        }

        if (AddException is not null)
        {
            throw AddException;
        }

        adds.Add((rawText, source, limit));

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            items.Insert(0, new HistoryItem(Guid.NewGuid(), DateTimeOffset.Now, source, rawText));
            while (items.Count > limit)
            {
                items.RemoveAt(items.Count - 1);
            }
        }
    }

    public Task ApplyLimitAsync(int limit)
    {
        while (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
        }

        return Task.CompletedTask;
    }

    public Task WaitForAddAsync()
    {
        return addReached.Task;
    }

    public void ReleaseAdd()
    {
        addRelease.SetResult();
    }
}
