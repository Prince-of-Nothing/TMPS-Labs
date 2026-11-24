namespace Watchlist.Behavioral.Observers;

public enum WatchlistChangeKind
{
    Added,
    Updated,
    Deleted,
    Imported,
    Reloaded,
}

public sealed record WatchlistChange(
    WatchlistChangeKind Kind,
    int? ItemId,
    string Message,
    DateTimeOffset Timestamp);
