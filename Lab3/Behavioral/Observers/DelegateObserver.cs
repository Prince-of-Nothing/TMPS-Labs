namespace Watchlist.Behavioral.Observers;

/// <summary>Adapter for a UI callback; keeps the UI free of observer plumbing.</summary>
public sealed class DelegateObserver(Action<WatchlistChange> callback) : IWatchlistObserver
{
    public void OnChanged(WatchlistChange change) => callback(change);
}
