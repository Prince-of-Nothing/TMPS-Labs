using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class DeleteMediaCommand(WatchlistEditor editor, MediaItem item) : IWatchlistCommand
{
    public string Description => $"Delete “{item.Title}”";

    public void Execute() => editor.Delete(item);

    public void Undo() => editor.Restore(item);
}
