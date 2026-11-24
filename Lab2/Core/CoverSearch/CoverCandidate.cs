namespace Watchlist.Core.CoverSearch;

public sealed record CoverCandidate(
    string Title,
    string ThumbnailUrl,
    string ImageUrl,
    string Provider);
