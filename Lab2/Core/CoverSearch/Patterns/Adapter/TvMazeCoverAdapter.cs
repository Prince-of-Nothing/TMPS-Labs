using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Adapters;

/// <summary>Adapter for TVmaze's nested show/image response.</summary>
public sealed class TvMazeCoverAdapter(TvMazeClient client) : ICoverCatalog
{
    public async Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var shows = await client.SearchAsync(query, cancellationToken: cancellationToken);
        return shows
            .Where(show => !string.IsNullOrWhiteSpace(show.ImageUrl))
            .Select(show => new CoverCandidate(
                show.Name,
                show.ThumbnailUrl ?? show.ImageUrl!,
                show.ImageUrl!,
                "TVmaze"))
            .ToArray();
    }
}
