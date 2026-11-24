using System.Text.Json;

namespace Watchlist.Core.CoverSearch.Providers;

public sealed record TvMazeShow(string Name, string? ThumbnailUrl, string? ImageUrl, string? Url);

/// <summary>Thin client for the public TVmaze search API.</summary>
public sealed class TvMazeClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<TvMazeShow>> SearchAsync(
        string query,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.tvmaze.com/search/shows?q={Uri.EscapeDataString(query)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<TvMazeShow>();
        foreach (var row in document.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("show", out var show))
            {
                continue;
            }

            var name = show.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "" : "";
            string? medium = null;
            string? original = null;
            if (show.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.Object)
            {
                medium = image.TryGetProperty("medium", out var mediumElement) ? mediumElement.GetString() : null;
                original = image.TryGetProperty("original", out var originalElement) ? originalElement.GetString() : null;
            }

            var urlValue = show.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
            if (name.Length > 0 && (medium is not null || original is not null))
            {
                result.Add(new TvMazeShow(name, medium, original ?? medium, urlValue));
                if (result.Count >= limit)
                {
                    break;
                }
            }
        }

        return result;
    }
}
