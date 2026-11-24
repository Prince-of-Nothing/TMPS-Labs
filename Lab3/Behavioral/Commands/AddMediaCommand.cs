using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class AddMediaCommand(WatchlistEditor editor, MediaItem item) : IWatchlistCommand
{
    public string Description => $"Add “{item.Title}”";

    public void Execute() => editor.Add(item);

    public void Undo() => editor.Delete(item);
}
