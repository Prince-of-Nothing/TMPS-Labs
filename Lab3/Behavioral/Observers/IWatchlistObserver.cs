namespace Watchlist.Behavioral.Observers;

public interface IWatchlistObserver
{
    void OnChanged(WatchlistChange change);
}
