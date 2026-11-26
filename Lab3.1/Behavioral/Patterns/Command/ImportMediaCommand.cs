using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class ImportMediaCommand(
    WatchlistEditor editor,
    IReadOnlyList<MediaItem> items,
    IProgress<ImportProgress>? progress = null) : IWatchlistCommand
{
    public string Description => $"Import {items.Count} entries";

    public void Execute() => editor.AddMany(items, progress);

    public void Undo() => editor.DeleteMany(items);
}
