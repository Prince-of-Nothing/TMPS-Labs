using Watchlist.Core.Domain;

namespace Watchlist.Core.Storage;

/// <summary>
/// In-memory view of the vault. Every mutation is written immediately and
/// the index is regenerated, so the folder is always self-describing.
/// </summary>
public sealed class WatchlistRepository
{
    private readonly IWatchlistStore _store;
    private readonly List<MediaItem> _items = [];
    private readonly List<VaultError> _errors = [];
    private readonly object _gate = new();

    public WatchlistRepository(IWatchlistStore store)
    {
        _store = store;
        Reload();
    }

    public IReadOnlyList<MediaItem> Items
    {
        get
        {
            lock (_gate)
            {
                return _items.ToArray();
            }
        }
    }

    public IReadOnlyList<VaultError> Errors
    {
        get
        {
            lock (_gate)
            {
                return _errors.ToArray();
            }
        }
    }

    public string RootPath => _store.RootPath;

    public int NextId
    {
        get
        {
            lock (_gate)
            {
                return _items.Count == 0 ? 1 : _items.Max(item => item.Id) + 1;
            }
        }
    }

    public void Reload()
    {
        lock (_gate)
        {
            var result = _store.Load();
            _items.Clear();
            _items.AddRange(result.Items);
            _errors.Clear();
            _errors.AddRange(result.Errors);
            TryWriteIndex();
        }
    }

    public MediaItem? Find(int id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(item => item.Id == id);
        }
    }

    public void Add(MediaItem item)
    {
        lock (_gate)
        {
            if (_items.Any(existing => existing.Id == item.Id))
            {
                throw new InvalidOperationException($"Entry #{item.Id} already exists.");
            }

            _store.Save(item);
            _items.Add(item);
            _items.Sort((left, right) => left.Id.CompareTo(right.Id));
            TryWriteIndex();
        }
    }

    public void Update(MediaItem item)
    {
        lock (_gate)
        {
            if (_items.All(existing => existing.Id != item.Id))
            {
                throw new InvalidOperationException($"Entry #{item.Id} was not found.");
            }

            item.Touch();
            _store.Save(item);
            TryWriteIndex();
        }
    }

    public void Delete(int id)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return;
            }

            _store.Delete(id);
            _items.RemoveAt(index);
            TryWriteIndex();
        }
    }

    /// <summary>Undo support: put a previously loaded entry back with its id.</summary>
    public void Restore(MediaItem item)
    {
        lock (_gate)
        {
            if (_items.Any(existing => existing.Id == item.Id))
            {
                throw new InvalidOperationException($"Entry #{item.Id} already exists.");
            }

            _store.Save(item);
            _items.Add(item);
            _items.Sort((left, right) => left.Id.CompareTo(right.Id));
            TryWriteIndex();
        }
    }

    private void TryWriteIndex()
    {
        try
        {
            _store.WriteIndex(_items);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _errors.Add(new VaultError(Path.Combine(_store.RootPath, "index.md"), ex.Message));
        }
    }
}
