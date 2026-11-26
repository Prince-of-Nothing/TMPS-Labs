using Watchlist.Behavioral.Observers;
using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Watchlist.Behavioral.Commands;

/// <summary>
/// Receiver. Commands describe what to do; this class owns the repository
/// calls and publishes one event per completed change.
/// </summary>
public sealed class WatchlistEditor(WatchlistRepository repository, WatchlistEvents events)
{
    public IReadOnlyList<MediaItem> Items => repository.Items;
    public IReadOnlyList<VaultError> Errors => repository.Errors;
    public string RootPath => repository.RootPath;
    public int NextId => repository.NextId;

    public MediaItem? Find(int id) => repository.Find(id);

    public void Add(MediaItem item)
    {
        repository.Add(item);
        events.Publish(WatchlistChangeKind.Added, item.Id, $"Added “{item.Title}”.");
    }

    public void AddMany(IEnumerable<MediaItem> items, IProgress<ImportProgress>? progress = null)
    {
        var all = items.ToArray();
        var added = new List<MediaItem>();
        try
        {
            foreach (var item in all)
            {
                repository.Add(item);
                added.Add(item);
                progress?.Report(new ImportProgress(added.Count, all.Length, item.Title));
            }
        }
        catch
        {
            foreach (var item in added)
            {
                repository.Delete(item.Id);
            }

            throw;
        }

        if (added.Count > 0)
        {
            events.Publish(
                WatchlistChangeKind.Imported,
                null,
                $"Imported {added.Count} entr{(added.Count == 1 ? "y" : "ies")}.");
        }
    }

    public void Update(MediaItem item, string message)
    {
        repository.Update(item);
        events.Publish(WatchlistChangeKind.Updated, item.Id, message);
    }

    public void Delete(MediaItem item)
    {
        repository.Delete(item.Id);
        events.Publish(WatchlistChangeKind.Deleted, item.Id, $"Deleted “{item.Title}”.");
    }

    public void DeleteMany(IReadOnlyCollection<MediaItem> items)
    {
        foreach (var item in items)
        {
            repository.Delete(item.Id);
        }

        if (items.Count > 0)
        {
            events.Publish(
                WatchlistChangeKind.Deleted,
                null,
                $"Removed {items.Count} imported entr{(items.Count == 1 ? "y" : "ies")}.");
        }
    }

    public void Restore(MediaItem item)
    {
        repository.Restore(item);
        events.Publish(WatchlistChangeKind.Added, item.Id, $"Restored “{item.Title}”.");
    }

    public void Reload()
    {
        repository.Reload();
        events.Publish(WatchlistChangeKind.Reloaded, null, "Reloaded the Markdown vault.");
    }
}
