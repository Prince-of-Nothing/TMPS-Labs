using Watchlist.Core.Domain;

namespace Lab3;

public sealed record EntryDraft(
    string Title,
    MediaKind Kind,
    string Category,
    string Tags,
    string Notes,
    string? Progress,
    string? CoverPath,
    string? CoverSourceUrl);
