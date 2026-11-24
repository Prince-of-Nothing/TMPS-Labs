using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.States;

public sealed class ConsumedState : IMediaState
{
    public MediaStatus Status => MediaStatus.Consumed;
    public string Name => "Consumed";

    public void Consume(MediaLifecycle lifecycle)
    {
        // Idempotent: consuming twice is not an error.
    }

    public void Rate(MediaLifecycle lifecycle, int rating)
    {
        lifecycle.Item.Rate(rating);
        lifecycle.MoveTo(new RatedState());
    }

    public void Reopen(MediaLifecycle lifecycle)
    {
        lifecycle.Item.Reopen();
        lifecycle.MoveTo(new PlannedState());
    }
}
