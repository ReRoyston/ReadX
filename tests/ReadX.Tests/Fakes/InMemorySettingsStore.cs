using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemorySettingsStore(AppSettings initial) : ISettingsStore
{
    public AppSettings Current { get; private set; } = initial;
    public int SaveCount { get; private set; }
    public Exception? SaveException { get; init; }

    public Task<AppSettings> LoadAsync()
    {
        return Task.FromResult(Current);
    }

    public Task SaveAsync(AppSettings settings)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        SaveCount++;
        Current = settings;
        return Task.CompletedTask;
    }
}
