using System.Globalization;
using System.Text;
using System.Text.Json;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Import;

/// <summary>
/// Adapter for AniList's public GraphQL API. No token is needed for a public
/// profile, and the score is requested as POINT_10 so it maps directly.
/// </summary>
public sealed class AniListImporter(HttpClient httpClient) : IWatchlistImporter
{
    public string Name => "AniList";

    private const string Query = """
        query Watchlist($user: String) {
          anime: MediaListCollection(userName: $user, type: ANIME) {
            lists {
              name
              entries {
                status
                score(format: POINT_10)
                progress
                media {
                  id
                  siteUrl
                  format
                  episodes
                  genres
                  title { romaji english native }
                  coverImage { extraLarge large }
                }
              }
            }
          }
          manga: MediaListCollection(userName: $user, type: MANGA) {
            lists {
              name
              entries {
                status
                score(format: POINT_10)
                progress
                progressVolumes
                media {
                  id
                  siteUrl
                  format
                  chapters
                  volumes
                  genres
                  title { romaji english native }
                  coverImage { extraLarge large }
                }
              }
            }
          }
        }
        """;

    public async Task<IReadOnlyList<ImportedMedia>> ImportAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var username = input.Trim();
        if (username.Length == 0)
        {
            throw new ImportException("Enter an AniList username.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://graphql.anilist.co")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { query = Query, variables = new { user = username } }),
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ImportException($"AniList returned {(int)response.StatusCode}.");
        }

        if (document.RootElement.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Array)
        {
            var message = string.Join(
                "; ",
                errors.EnumerateArray()
                    .Select(error => error.TryGetProperty("message", out var text) ? text.GetString() : null)
                    .Where(text => text is not null));
            throw new ImportException(string.IsNullOrWhiteSpace(message) ? "AniList rejected the request." : message);
        }

        if (!document.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object)
        {
            throw new ImportException("AniList did not return a public list for that username.");
        }

        var result = new List<ImportedMedia>();
        ReadCollection(data, "anime", MediaKind.Anime, result);
        ReadCollection(data, "manga", MediaKind.Manga, result);
        return result;
    }

    private static void ReadCollection(
        JsonElement data,
        string property,
        MediaKind kind,
        List<ImportedMedia> result)
    {
        if (!data.TryGetProperty(property, out var collection) ||
            collection.ValueKind != JsonValueKind.Object ||
            !collection.TryGetProperty("lists", out var lists) ||
            lists.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var list in lists.EnumerateArray())
        {
            if (!list.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("media", out var media) || media.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var title = ReadTitle(media);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var statusText = ReadString(entry, "status") ?? "";
                var score = ReadInt(entry, "score");
                var rating = score is >= 1 and <= 10 ? score : null;
                var status = statusText.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)
                    ? rating is null ? MediaStatus.Consumed : MediaStatus.Rated
                    : MediaStatus.Planned;
                var progress = ReadProgress(entry, media, kind);
                var format = ReadString(media, "format") ?? (kind == MediaKind.Anime ? "Anime" : "Manga");
                var genres = ReadStringArray(media, "genres");
                var cover = ReadCover(media);

                result.Add(new ImportedMedia(
                    title.Trim(),
                    kind,
                    DisplayName(format),
                    genres,
                    status,
                    status == MediaStatus.Rated ? rating : null,
                    progress,
                    cover,
                    ReadString(media, "siteUrl"),
                    statusText));
            }
        }
    }

    private static string? ReadTitle(JsonElement media)
    {
        if (!media.TryGetProperty("title", out var title) || title.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(title, "english")
               ?? ReadString(title, "romaji")
               ?? ReadString(title, "native");
    }

    private static string? ReadCover(JsonElement media)
    {
        if (!media.TryGetProperty("coverImage", out var cover) || cover.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(cover, "extraLarge") ?? ReadString(cover, "large");
    }

    private static string? ReadProgress(JsonElement entry, JsonElement media, MediaKind kind)
    {
        var progress = ReadInt(entry, "progress");
        var total = kind == MediaKind.Anime
            ? ReadInt(media, "episodes")
            : ReadInt(media, "chapters");
        if (progress is null && total is null)
        {
            var volumes = ReadInt(entry, "progressVolumes");
            var volumeTotal = ReadInt(media, "volumes");
            if (volumes is null && volumeTotal is null)
            {
                return null;
            }

            return volumeTotal is null ? $"{volumes ?? 0}" : $"{volumes ?? 0}/{volumeTotal}";
        }

        return total is null ? $"{progress ?? 0}" : $"{progress ?? 0}/{total}";
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : null;

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? "")
            .Where(item => item.Length > 0)
            .Take(10)
            .ToArray();
    }

    private static string DisplayName(string value) => value.ToUpperInvariant() switch
    {
        "TV" => "TV",
        "OVA" => "OVA",
        "ONA" => "ONA",
        "MOVIE" => "Movie",
        "SPECIAL" => "Special",
        "MANGA" => "Manga",
        "NOVEL" => "Novel",
        "ONE_SHOT" => "One Shot",
        _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').ToLowerInvariant()),
    };
}
