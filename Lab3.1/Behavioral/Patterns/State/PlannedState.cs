using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.States;

public sealed class PlannedState : IMediaState
{
    public MediaStatus Status => MediaStatus.Planned;
    public string Name => "Planned";

    public void Consume(MediaLifecycle lifecycle)
    {
        lifecycle.Item.MarkConsumed();
        lifecycle.MoveTo(new ConsumedState());
    }

    public void Rate(MediaLifecycle lifecycle, int rating) =>
        throw new InvalidOperationException("Mark the media as consumed before rating it.");

    public void Reopen(MediaLifecycle lifecycle)
    {
        // Already planned; repeating the command is harmless.
    }
}
