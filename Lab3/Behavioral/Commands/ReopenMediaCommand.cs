using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class ReopenMediaCommand(WatchlistEditor editor, MediaItem item)
    : LifecycleCommand(editor, item)
{
    public override string Description => $"Reopen “{Item.Title}”";

    public override void Execute()
    {
        var lifecycle = new MediaLifecycle(Item);
        lifecycle.Reopen();
        Editor.Update(Item, $"Reopened “{Item.Title}”.");
    }
}
