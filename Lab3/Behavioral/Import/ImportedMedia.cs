using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Import;

public sealed record ImportedMedia(
    string Title,
    MediaKind Kind,
    string Category,
    IReadOnlyList<string> Tags,
    MediaStatus Status,
    int? Rating,
    string? Progress,
    string? CoverSourceUrl,
    string? SourceUrl,
    string SourceStatus);
