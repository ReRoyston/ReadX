using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemorySettingsStore(AppSettings initial) : ISettingsStore
{
    public AppSettings Current { get; private set; } = initial;

    public Task<AppSettings> LoadAsync()
    {
        return Task.FromResult(Current);
    }

    public Task SaveAsync(AppSettings settings)
    {
        Current = settings;
        return Task.CompletedTask;
    }
}
