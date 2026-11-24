namespace Watchlist.Behavioral.Observers;

/// <summary>
/// Subject. Observers subscribe and unsubscribe at runtime; a failing
/// observer is reported but never blocks the save that triggered it.
/// </summary>
public sealed class WatchlistEvents
{
    private readonly List<IWatchlistObserver> _observers = [];
    private readonly object _gate = new();
    private bool _publishing;

    public event EventHandler<Exception>? ObserverFailed;

    public IReadOnlyList<IWatchlistObserver> Observers
    {
        get
        {
            lock (_gate)
            {
                return _observers.ToArray();
            }
        }
    }

    public void Subscribe(IWatchlistObserver observer)
    {
        lock (_gate)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    public void Unsubscribe(IWatchlistObserver observer)
    {
        lock (_gate)
        {
            _observers.Remove(observer);
        }
    }

    public void Publish(WatchlistChangeKind kind, int? itemId, string message)
    {
        var change = new WatchlistChange(kind, itemId, message, DateTimeOffset.Now);
        IWatchlistObserver[] snapshot;
        lock (_gate)
        {
            if (_publishing)
            {
                throw new InvalidOperationException("Observers must not publish another change.");
            }

            _publishing = true;
            snapshot = _observers.ToArray();
        }

        try
        {
            foreach (var observer in snapshot)
            {
                try
                {
                    observer.OnChanged(change);
                }
                catch (Exception ex)
                {
                    ObserverFailed?.Invoke(this, ex);
                }
            }
        }
        finally
        {
            lock (_gate)
            {
                _publishing = false;
            }
        }
    }
}
