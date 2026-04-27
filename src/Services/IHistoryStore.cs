using ReadX.Models;

namespace ReadX.Services;

public interface IHistoryStore
{
    Task<IReadOnlyList<HistoryItem>> LoadAsync();
    Task AddAsync(string rawText, HistorySource source, int limit);
    Task ApplyLimitAsync(int limit);
}
