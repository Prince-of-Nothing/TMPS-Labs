using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class ConsumeMediaCommand(WatchlistEditor editor, MediaItem item)
    : LifecycleCommand(editor, item)
{
    public override string Description => $"Consume “{Item.Title}”";

    public override void Execute()
    {
        var lifecycle = new MediaLifecycle(Item);
        lifecycle.Consume();
        Editor.Update(Item, $"Consumed “{Item.Title}”.");
    }
}
