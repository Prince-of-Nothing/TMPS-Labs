using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Commands;

public sealed record MediaItemEdit(
    string Title,
    MediaKind Kind,
    string Category,
    IReadOnlyList<string> Tags,
    string Notes,
    string? Progress,
    string? CoverPath = null,
    string? CoverSourceUrl = null)
{
    public static MediaItemEdit From(MediaItem item) =>
        new(
            item.Title,
            item.Kind,
            item.Category,
            item.Tags.ToArray(),
            item.Notes,
            item.Progress,
            item.CoverPath,
            item.CoverSourceUrl);

    public void ApplyTo(MediaItem item)
    {
        item.Edit(Title, Kind, Category, Tags, Notes, Progress);
        item.SetCover(CoverPath, CoverSourceUrl);
    }
}
