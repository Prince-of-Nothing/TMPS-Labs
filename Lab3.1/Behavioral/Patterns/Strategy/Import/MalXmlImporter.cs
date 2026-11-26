using System.Xml;
using System.Xml.Linq;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Import;

/// <summary>
/// Adapter for the XML file MAL itself produces from Profile → Export/Import.
/// This avoids OAuth and works fully offline.
/// </summary>
public sealed class MalXmlImporter : IWatchlistImporter
{
    public string Name => "MAL XML";

    public async Task<IReadOnlyList<ImportedMedia>> ImportAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ImportException("Choose a MAL export file.");
        }

        string xml;
        if (input.TrimStart().StartsWith('<'))
        {
            xml = input;
        }
        else
        {
            if (!File.Exists(input))
            {
                throw new ImportException($"The MAL export file was not found: {input}");
            }

            xml = await File.ReadAllTextAsync(input, cancellationToken);
        }

        return ParseXml(xml);
    }

    public IReadOnlyList<ImportedMedia> ParseXml(string xml)
    {
        XDocument document;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });
            document = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new ImportException("That file is not valid MAL XML.", ex);
        }

        var root = document.Root;
        if (root is null || !root.Name.LocalName.Equals("myanimelist", StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportException("The XML does not look like a MAL export (root element is not myanimelist).");
        }

        var result = new List<ImportedMedia>();
        foreach (var element in root.Elements().Where(element =>
                     element.Name.LocalName is "anime" or "manga"))
        {
            var title = Value(element, "series_title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var isAnime = element.Name.LocalName == "anime";
            var kind = isAnime ? MediaKind.Anime : MediaKind.Manga;
            var type = Value(element, "series_type") ?? (isAnime ? "Anime" : "Manga");
            var sourceStatus = Value(element, "my_status") ?? "";
            var score = ParseInt(Value(element, "my_score"));
            var rating = score is >= 1 and <= 10 ? score : null;
            var status = sourceStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                ? rating is null ? MediaStatus.Consumed : MediaStatus.Rated
                : MediaStatus.Planned;

            var tags = new List<string> { "mal" };
            if (!string.IsNullOrWhiteSpace(type))
            {
                tags.Add(type);
            }

            result.Add(new ImportedMedia(
                title.Trim(),
                kind,
                type,
                tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                status,
                status == MediaStatus.Rated ? rating : null,
                ReadProgress(element, isAnime),
                NullIfEmpty(Value(element, "series_image")),
                ReadSourceUrl(element, isAnime),
                sourceStatus));
        }

        return result;
    }

    private static string? ReadProgress(XElement element, bool isAnime)
    {
        var watched = ParseInt(Value(element, isAnime ? "my_watched_episodes" : "my_read_chapters"));
        var total = ParseInt(Value(element, isAnime ? "series_episodes" : "series_chapters"));
        if (watched is null && total is null)
        {
            var volumes = ParseInt(Value(element, "my_read_volumes"));
            var volumeTotal = ParseInt(Value(element, "series_volumes"));
            if (volumes is null && volumeTotal is null)
            {
                return null;
            }

            return volumeTotal is null ? $"{volumes ?? 0}" : $"{volumes ?? 0}/{volumeTotal}";
        }

        return total is null ? $"{watched ?? 0}" : $"{watched ?? 0}/{total}";
    }

    private static string? ReadSourceUrl(XElement element, bool isAnime)
    {
        var id = ParseInt(Value(element, isAnime ? "series_animedb_id" : "series_mangadb_id"));
        return id is null
            ? null
            : isAnime
                ? $"https://myanimelist.net/anime/{id}"
                : $"https://myanimelist.net/manga/{id}";
    }

    private static string? Value(XElement element, string name) =>
        NullIfEmpty(element.Elements()
            .FirstOrDefault(child => child.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value);

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var number) ? number : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
