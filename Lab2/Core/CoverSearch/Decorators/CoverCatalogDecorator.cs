using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Decorators;

/// <summary>Base Decorator: same interface, wraps another catalog.</summary>
public abstract class CoverCatalogDecorator(ICoverCatalog inner) : ICoverCatalog
{
    protected ICoverCatalog Inner { get; } = inner;

    public abstract Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default);
}
