using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Observers;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class ObserverTests
{
    private sealed class CountingObserver : IWatchlistObserver
    {
        public int Calls { get; private set; }
        public WatchlistChange? Last { get; private set; }

        public void OnChanged(WatchlistChange change)
        {
            Calls++;
            Last = change;
        }
    }

    [Fact]
    public void All_subscribers_are_notified_and_unsubscribed_ones_are_not()
    {
        using var fixture = new BehavioralFixture();
        var first = new CountingObserver();
        var second = new CountingObserver();
        fixture.Events.Subscribe(first);
        fixture.Events.Subscribe(second);

        fixture.Editor.Add(fixture.NewItem());
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);

        fixture.Events.Unsubscribe(second);
        fixture.Editor.Add(fixture.NewItem("Second"));

        Assert.Equal(2, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public void Statistics_observer_recomputes_counts_and_average()
    {
        using var fixture = new BehavioralFixture();
        var stats = new StatisticsObserver(() => fixture.Editor.Items);
        fixture.Events.Subscribe(stats);

        var planned = fixture.AddItem("Planned");
        var consumed = fixture.AddItem("Consumed");
        consumed.MarkConsumed();
        fixture.Editor.Update(consumed, "consumed");
        var rated = fixture.AddItem("Rated");
        rated.MarkConsumed();
        rated.Rate(8);
        fixture.Editor.Update(rated, "rated");

        fixture.Editor.Delete(planned);

        Assert.Equal(2, stats.Statistics.Total);
        Assert.Equal(0, stats.Statistics.Planned);
        Assert.Equal(1, stats.Statistics.Consumed);
        Assert.Equal(1, stats.Statistics.Rated);
        Assert.Equal(8, stats.Statistics.AverageRating);
    }

    [Fact]
    public void Activity_log_keeps_the_latest_entries()
    {
        using var fixture = new BehavioralFixture();
        var log = new ActivityLogObserver(capacity: 2);
        fixture.Events.Subscribe(log);

        fixture.Editor.Add(fixture.NewItem("One"));
        fixture.Editor.Add(fixture.NewItem("Two"));
        fixture.Editor.Add(fixture.NewItem("Three"));

        Assert.Equal(2, log.Entries.Count);
        Assert.Contains("Three", log.Entries[1]);
    }

    [Fact]
    public void A_failing_observer_does_not_block_the_change_or_other_observers()
    {
        using var fixture = new BehavioralFixture();
        var errors = new List<Exception>();
        var good = new CountingObserver();
        fixture.Events.ObserverFailed += (_, error) => errors.Add(error);
        fixture.Events.Subscribe(new DelegateObserver(_ => throw new InvalidOperationException("bad")));
        fixture.Events.Subscribe(good);

        fixture.Editor.Add(fixture.NewItem());

        Assert.Single(errors);
        Assert.Equal(1, good.Calls);
        Assert.Single(fixture.Editor.Items);
    }

    [Fact]
    public void An_observer_cannot_publish_a_nested_change()
    {
        using var fixture = new BehavioralFixture();
        fixture.Events.Subscribe(new DelegateObserver(_ =>
            fixture.Events.Publish(WatchlistChangeKind.Updated, null, "nested")));
        var errors = new List<Exception>();
        fixture.Events.ObserverFailed += (_, error) => errors.Add(error);

        fixture.Editor.Add(fixture.NewItem());

        Assert.Single(errors);
        Assert.IsType<InvalidOperationException>(errors[0]);
    }
}
