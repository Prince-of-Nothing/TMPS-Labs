using Microsoft.Maui.Controls;
using Watchlist.Core.Domain;

namespace Lab3;

/// <summary>View model for one card; rebuilt after every notification.</summary>
public sealed class ItemRow
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string StatusText { get; init; }
    public required string StatusColor { get; init; }
    public required string ProgressText { get; init; }
    public required string NotesPreview { get; init; }
    public required bool HasNotes { get; init; }
    public required bool IsPlanned { get; init; }
    public required bool CanRate { get; init; }
    public required bool CanReopen { get; init; }
    public ImageSource? Cover { get; init; }
}
