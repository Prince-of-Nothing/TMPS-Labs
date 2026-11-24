using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed class SetCoverCommand(
    WatchlistEditor editor,
    MediaItem item,
    string relativePath,
    string sourceUrl) : IWatchlistCommand
{
    private readonly string? _oldPath = item.CoverPath;
    private readonly string? _oldSource = item.CoverSourceUrl;

    public string Description => $"Change cover for “{item.Title}”";

    public void Execute()
    {
        item.SetCover(relativePath, sourceUrl);
        editor.Update(item, $"Changed the cover for “{item.Title}”.");
    }

    public void Undo()
    {
        item.SetCover(_oldPath, _oldSource);
        editor.Update(item, $"Restored the cover for “{item.Title}”.");
    }
}
