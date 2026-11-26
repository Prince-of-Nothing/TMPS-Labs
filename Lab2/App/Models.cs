using Microsoft.Maui.Controls;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.Domain;

namespace Lab2;

/// <summary>View model for one card; rebuilt after every change.</summary>
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

/// <summary>What the edit page hands back to the main page.</summary>
public sealed record EntryDraft(
    string Title,
    MediaKind Kind,
    string Category,
    string Tags,
    string Notes,
    string? Progress);

/// <summary>One search result in the cover picker.</summary>
public sealed record CoverRow(CoverCandidate Candidate, string Title, string Provider, ImageSource? Thumbnail);
