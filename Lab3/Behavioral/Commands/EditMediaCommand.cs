using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class EditMediaCommand(
    WatchlistEditor editor,
    MediaItem item,
    MediaItemEdit newValue) : IWatchlistCommand
{
    private readonly MediaItemEdit _oldValue = MediaItemEdit.From(item);

    public string Description => $"Edit “{item.Title}”";

    public void Execute()
    {
        newValue.ApplyTo(item);
        editor.Update(item, $"Edited “{item.Title}”.");
    }

    public void Undo()
    {
        _oldValue.ApplyTo(item);
        editor.Update(item, $"Restored “{item.Title}”.");
    }
}
