using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class StateTests
{
    private static MediaItem Item() =>
        new MediaItemBuilder().WithTitle("State test").Build(1);

    [Fact]
    public void Planned_rejects_rating()
    {
        var lifecycle = new MediaLifecycle(Item());

        Assert.Equal("Planned", lifecycle.StateName);
        Assert.Throws<InvalidOperationException>(() => lifecycle.Rate(8));
        Assert.Equal(MediaStatus.Planned, lifecycle.Item.Status);
    }

    [Fact]
    public void Consume_moves_to_consumed_and_rate_moves_to_rated()
    {
        var lifecycle = new MediaLifecycle(Item());

        lifecycle.Consume();
        Assert.Equal("Consumed", lifecycle.StateName);
        Assert.Equal(MediaStatus.Consumed, lifecycle.Item.Status);

        lifecycle.Rate(9);
        Assert.Equal("Rated", lifecycle.StateName);
        Assert.Equal(MediaStatus.Rated, lifecycle.Item.Status);
        Assert.Equal(9, lifecycle.Item.Rating);
    }

    [Fact]
    public void Reopen_from_rated_clears_the_rating_and_returns_to_planned()
    {
        var lifecycle = new MediaLifecycle(Item());
        lifecycle.Consume();
        lifecycle.Rate(7);

        lifecycle.Reopen();

        Assert.Equal("Planned", lifecycle.StateName);
        Assert.Null(lifecycle.Item.Rating);
    }

    [Fact]
    public void Restore_rejects_an_inconsistent_snapshot()
    {
        var lifecycle = new MediaLifecycle(Item());

        Assert.Throws<ArgumentException>(() => lifecycle.Restore(MediaStatus.Planned, 8));
    }
}
