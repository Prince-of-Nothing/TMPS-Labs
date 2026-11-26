using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.States;

/// <summary>
/// State context. It delegates every transition to the current state, so the
/// valid transitions live in one small class per status.
/// </summary>
public sealed class MediaLifecycle
{
    private IMediaState _state;

    public MediaLifecycle(MediaItem item)
    {
        Item = item;
        _state = StateFor(item.Status);
    }

    public MediaItem Item { get; }
    public MediaStatus Status => _state.Status;
    public string StateName => _state.Name;

    public void Consume() => _state.Consume(this);
    public void Rate(int rating) => _state.Rate(this, rating);
    public void Reopen() => _state.Reopen(this);

    /// <summary>Used by Undo to put an entry back exactly where it was.</summary>
    public void Restore(MediaStatus status, int? rating)
    {
        Item.RestoreState(status, rating);
        _state = StateFor(status);
    }

    internal void MoveTo(IMediaState state) => _state = state;

    private static IMediaState StateFor(MediaStatus status) => status switch
    {
        MediaStatus.Planned => new PlannedState(),
        MediaStatus.Consumed => new ConsumedState(),
        MediaStatus.Rated => new RatedState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}
