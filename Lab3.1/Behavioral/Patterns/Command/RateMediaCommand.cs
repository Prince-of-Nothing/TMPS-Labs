using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class RateMediaCommand(WatchlistEditor editor, MediaItem item, int rating)
    : LifecycleCommand(editor, item)
{
    public override string Description => $"Rate “{Item.Title}” {rating}/10";

    public override void Execute()
    {
        var lifecycle = new MediaLifecycle(Item);
        lifecycle.Rate(rating);
        Editor.Update(Item, $"Rated “{Item.Title}” {rating}/10.");
    }
}
