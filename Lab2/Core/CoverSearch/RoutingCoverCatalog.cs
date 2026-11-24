using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch;

/// <summary>
/// Picks the best source for a media kind and falls back to the general
/// source when the specialised provider has nothing or is unavailable.
/// </summary>
public sealed class RoutingCoverCatalog(
    IReadOnlyDictionary<MediaKind, ICoverCatalog> byKind,
    ICoverCatalog fallback) : ICoverCatalog
{
    public async Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default)
    {
        var primary = byKind.GetValueOrDefault(kind) ?? fallback;
        try
        {
            var hits = await primary.SearchAsync(query, kind, cancellationToken);
            if (hits.Count > 0 || ReferenceEquals(primary, fallback))
            {
                return hits;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or CoverSearchException or TaskCanceledException)
        {
            if (ReferenceEquals(primary, fallback))
            {
                throw;
            }
        }

        try
        {
            return await fallback.SearchAsync(query, kind, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CoverSearchException("Cover search is unavailable right now.", ex);
        }
    }
}
