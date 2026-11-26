namespace Watchlist.Behavioral.Observers;

/// <summary>Observer that keeps a short human-readable activity feed.</summary>
public sealed class ActivityLogObserver(int capacity = 50) : IWatchlistObserver
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void OnChanged(WatchlistChange change)
    {
        _entries.Add($"{change.Timestamp:HH:mm:ss}  {change.Message}");
        if (_entries.Count > capacity)
        {
            _entries.RemoveRange(0, _entries.Count - capacity);
        }
    }

    public void Clear() => _entries.Clear();
}
