using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Adapters;

/// <summary>
/// Adapter: maps the provider's WikimediaImage model to the CoverCandidate
/// target the rest of the app understands.
/// </summary>
public sealed class WikimediaCoverAdapter(WikimediaCommonsClient client) : ICoverCatalog
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

        var images = await client.SearchAsync(query, cancellationToken: cancellationToken);
        return images
            .Where(image => image.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .Where(image => !image.MimeType.Contains("svg", StringComparison.OrdinalIgnoreCase))
            .Select(image => new CoverCandidate(
                CleanTitle(image.Title),
                image.ThumbnailUrl,
                image.ImageUrl,
                "Wikimedia Commons"))
            .ToArray();
    }

    private static string CleanTitle(string title)
    {
        var value = title.StartsWith("File:", StringComparison.OrdinalIgnoreCase)
            ? title[5..]
            : title;
        var dot = value.LastIndexOf('.');
        return dot > 0 ? value[..dot].Replace('_', ' ') : value.Replace('_', ' ');
    }
}
