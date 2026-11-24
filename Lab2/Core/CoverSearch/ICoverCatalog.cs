using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch;

/// <summary>Target interface used by the client for every cover provider.</summary>
public interface ICoverCatalog
{
    Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default);
}
