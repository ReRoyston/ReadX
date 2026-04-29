namespace ReadX.Models;

public sealed record HistoryItem(Guid Id, DateTimeOffset CreatedAt, HistorySource Source, string RawText);
