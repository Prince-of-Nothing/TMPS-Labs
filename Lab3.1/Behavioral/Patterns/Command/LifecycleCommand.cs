using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

/// <summary>
/// Shared undo snapshot for the three lifecycle commands. Undo restores the
/// exact previous status and rating instead of guessing a transition.
/// </summary>
public abstract class LifecycleCommand : IWatchlistCommand
{
    private readonly WatchlistEditor _editor;
    private readonly MediaStatus _previousStatus;
    private readonly int? _previousRating;

    protected LifecycleCommand(WatchlistEditor editor, MediaItem item)
    {
        _editor = editor;
        Item = item;
        _previousStatus = item.Status;
        _previousRating = item.Rating;
    }

    public abstract string Description { get; }

    public abstract void Execute();

    public void Undo()
    {
        var lifecycle = new MediaLifecycle(Item);
        lifecycle.Restore(_previousStatus, _previousRating);
        _editor.Update(Item, $"Restored “{Item.Title}” to {_previousStatus.ToString().ToLowerInvariant()}.");
    }

    protected WatchlistEditor Editor => _editor;
    protected MediaItem Item { get; }
}
