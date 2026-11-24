using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class ImportMediaCommand(
    WatchlistEditor editor,
    IReadOnlyList<MediaItem> items) : IWatchlistCommand
{
    public string Description => $"Import {items.Count} entries";

    public void Execute() => editor.AddMany(items);

    public void Undo() => editor.DeleteMany(items);
}
