using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.States;

public sealed class RatedState : IMediaState
{
    public MediaStatus Status => MediaStatus.Rated;
    public string Name => "Rated";

    public void Consume(MediaLifecycle lifecycle)
    {
        // Already consumed; keep the rating.
    }

    public void Rate(MediaLifecycle lifecycle, int rating)
    {
        lifecycle.Item.Rate(rating);
    }

    public void Reopen(MediaLifecycle lifecycle)
    {
        lifecycle.Item.Reopen();
        lifecycle.MoveTo(new PlannedState());
    }
}
