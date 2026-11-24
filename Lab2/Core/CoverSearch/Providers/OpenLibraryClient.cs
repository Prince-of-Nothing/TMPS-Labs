using System.Text.Json;

namespace Watchlist.Core.CoverSearch.Providers;

public sealed record OpenLibraryBook(string Title, int? CoverId, string? Author);

/// <summary>Thin client for the public Open Library search API.</summary>
public sealed class OpenLibraryClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<OpenLibraryBook>> SearchAsync(
        string query,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var url =
            "https://openlibrary.org/search.json" +
            $"?q={Uri.EscapeDataString(query)}&limit={Math.Clamp(limit, 1, 40)}" +
            "&fields=title,cover_i,author_name";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("docs", out var docs) ||
            docs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<OpenLibraryBook>();
        foreach (var doc in docs.EnumerateArray())
        {
            var title = doc.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "" : "";
            int? coverId = doc.TryGetProperty("cover_i", out var coverElement) &&
                           coverElement.TryGetInt32(out var cover)
                ? cover
                : null;
            string? author = doc.TryGetProperty("author_name", out var authors) &&
                             authors.ValueKind == JsonValueKind.Array &&
                             authors.GetArrayLength() > 0
                ? authors[0].GetString()
                : null;
            if (title.Length > 0)
            {
                result.Add(new OpenLibraryBook(title, coverId, author));
            }
        }

        return result;
    }
}
