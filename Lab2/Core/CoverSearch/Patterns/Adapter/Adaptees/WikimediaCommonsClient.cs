using System.Text.Json;

namespace Watchlist.Core.CoverSearch.Providers;

public sealed record WikimediaImage(string Title, string ThumbnailUrl, string ImageUrl, string MimeType);

/// <summary>
/// Thin client for the public Wikimedia Commons API. It intentionally models
/// the provider's response instead of our CoverCandidate target.
/// </summary>
public sealed class WikimediaCommonsClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<WikimediaImage>> SearchAsync(
        string query,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var url =
            "https://commons.wikimedia.org/w/api.php" +
            "?action=query&generator=search" +
            $"&gsrsearch={Uri.EscapeDataString(query)}" +
            "&gsrnamespace=6" +
            $"&gsrlimit={Math.Clamp(limit, 1, 40)}" +
            "&prop=imageinfo&iiprop=url%7Cmime&iiurlwidth=480" +
            "&format=json&formatversion=2";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
            !queryElement.TryGetProperty("pages", out var pages) ||
            pages.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<WikimediaImage>();
        foreach (var page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("imageinfo", out var imageInfo) ||
                imageInfo.ValueKind != JsonValueKind.Array ||
                imageInfo.GetArrayLength() == 0)
            {
                continue;
            }

            var info = imageInfo[0];
            var title = page.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "" : "";
            var thumb = info.TryGetProperty("thumburl", out var thumbElement) ? thumbElement.GetString() ?? "" : "";
            var image = info.TryGetProperty("url", out var urlElement) ? urlElement.GetString() ?? "" : "";
            var mime = info.TryGetProperty("mime", out var mimeElement) ? mimeElement.GetString() ?? "" : "";
            if (thumb.Length > 0 && image.Length > 0)
            {
                result.Add(new WikimediaImage(title, thumb, image, mime));
            }
        }

        return result;
    }
}
