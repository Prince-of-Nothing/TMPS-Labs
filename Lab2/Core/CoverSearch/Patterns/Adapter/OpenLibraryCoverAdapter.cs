using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch.Adapters;

/// <summary>
/// Adapter for Open Library's search model. Books without a cover id are
/// simply omitted instead of producing a broken image link.
/// </summary>
public sealed class OpenLibraryCoverAdapter(OpenLibraryClient client) : ICoverCatalog
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

        var books = await client.SearchAsync(query, cancellationToken: cancellationToken);
        return books
            .Where(book => book.CoverId is > 0)
            .Select(book => new CoverCandidate(
                string.IsNullOrWhiteSpace(book.Author)
                    ? book.Title
                    : $"{book.Title} — {book.Author}",
                $"https://covers.openlibrary.org/b/id/{book.CoverId}-M.jpg",
                $"https://covers.openlibrary.org/b/id/{book.CoverId}-L.jpg",
                "Open Library"))
            .ToArray();
    }
}
