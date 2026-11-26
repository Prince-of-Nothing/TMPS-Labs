using System.Collections.Concurrent;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Decorators;

/// <summary>Decorator: avoids asking the same provider twice.</summary>
public sealed class CachingCoverCatalog(ICoverCatalog inner) : CoverCatalogDecorator(inner)
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<CoverCandidate>> _cache = new();

    public override async Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default)
    {
        var key = $"{kind}:{query.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var hits = await Inner.SearchAsync(query, kind, cancellationToken);
        if (hits.Count > 0)
        {
            _cache[key] = hits;
        }

        return hits;
    }

    public void Clear() => _cache.Clear();
}
