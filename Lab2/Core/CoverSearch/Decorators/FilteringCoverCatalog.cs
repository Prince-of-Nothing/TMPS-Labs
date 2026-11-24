using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Decorators;

/// <summary>
/// Decorator: keeps only usable cover links. It runs before the UI, so no
/// consumer has to repeat these checks.
/// </summary>
public sealed class FilteringCoverCatalog(ICoverCatalog inner) : CoverCatalogDecorator(inner)
{
    private static readonly string[] BlockedExtensions = [".svg", ".tif", ".tiff", ".pdf"];

    public override async Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default)
    {
        var hits = await Inner.SearchAsync(query, kind, cancellationToken);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return hits
            .Where(hit => !string.IsNullOrWhiteSpace(hit.Title))
            .Where(hit => IsHttpUrl(hit.ImageUrl))
            .Where(hit => !BlockedExtensions.Any(extension =>
                Uri.TryCreate(hit.ImageUrl, UriKind.Absolute, out var uri) &&
                uri.AbsolutePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .Where(hit => seen.Add(hit.ImageUrl))
            .Take(20)
            .ToArray();
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
