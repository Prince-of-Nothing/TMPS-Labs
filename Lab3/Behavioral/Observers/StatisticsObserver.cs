using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Observers;

public sealed record WatchlistStatistics(
    int Total,
    int Planned,
    int Consumed,
    int Rated,
    double AverageRating)
{
    public static readonly WatchlistStatistics Empty = new(0, 0, 0, 0, 0);
}

/// <summary>
/// Observer that recomputes the counters whenever the library changes.
/// It only reads a snapshot, so it cannot trigger another notification.
/// </summary>
public sealed class StatisticsObserver(Func<IReadOnlyList<MediaItem>> snapshot) : IWatchlistObserver
{
    public event EventHandler? Updated;

    public WatchlistStatistics Statistics { get; private set; } = WatchlistStatistics.Empty;
    public WatchlistChange? LastChange { get; private set; }

    public void OnChanged(WatchlistChange change)
    {
        var items = snapshot();
        var rated = items.Where(item => item.Rating is not null).Select(item => item.Rating!.Value).ToArray();
        Statistics = new WatchlistStatistics(
            items.Count,
            items.Count(item => item.Status == MediaStatus.Planned),
            items.Count(item => item.Status == MediaStatus.Consumed),
            items.Count(item => item.Status == MediaStatus.Rated),
            rated.Length == 0 ? 0 : rated.Average());
        LastChange = change;
        Updated?.Invoke(this, EventArgs.Empty);
    }
}
