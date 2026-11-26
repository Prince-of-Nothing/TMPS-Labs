using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Import;

/// <summary>
/// Imports a generic CSV exported by another service. Only Title is required;
/// Kind, Category, Tags, Status, Rating, Progress, Cover and Source are
/// optional, so a hand-made list works too.
/// </summary>
public sealed class CsvWatchlistImporter : IWatchlistImporter
{
    public string Name => "CSV file";

    public async Task<IReadOnlyList<ImportedMedia>> ImportAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ImportException("Choose a CSV file.");
        }

        var text = input;
        if (!input.Contains('\n') && File.Exists(input))
        {
            text = await File.ReadAllTextAsync(input, cancellationToken);
        }
        else if (!input.Contains('\n') && !input.Contains(','))
        {
            throw new ImportException($"The CSV file was not found: {input}");
        }

        return Parse(text);
    }

    public IReadOnlyList<ImportedMedia> Parse(string csv)
    {
        var rows = ParseRows(csv);
        if (rows.Count < 2)
        {
            throw new ImportException("The CSV needs a header and at least one data row.");
        }

        var headers = rows[0]
            .Select((header, index) => (Name: header.Trim(), Index: index))
            .Where(header => header.Name.Length > 0)
            .ToDictionary(header => header.Name, header => header.Index, StringComparer.OrdinalIgnoreCase);

        if (!headers.ContainsKey("Title"))
        {
            throw new ImportException("The CSV needs a Title column.");
        }

        var result = new List<ImportedMedia>();
        foreach (var row in rows.Skip(1))
        {
            var title = Field(row, headers, "Title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var rating = ParseRating(Field(row, headers, "Rating"));
            var statusText = Field(row, headers, "Status");
            var status = MapStatus(statusText, rating);
            var kind = MapKind(Field(row, headers, "Kind"));
            var tags = (Field(row, headers, "Tags") ?? "")
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            result.Add(new ImportedMedia(
                title,
                kind,
                Field(row, headers, "Category") ?? "Imported",
                tags,
                status,
                status == MediaStatus.Rated ? rating : null,
                NullIfEmpty(Field(row, headers, "Progress")),
                NullIfEmpty(Field(row, headers, "Cover")),
                NullIfEmpty(Field(row, headers, "Source")),
                statusText ?? "CSV"));
        }

        if (result.Count == 0)
        {
            throw new ImportException("The CSV did not contain any usable titles.");
        }

        return result;
    }

    private static string? Field(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headers,
        string name) =>
        headers.TryGetValue(name, out var index) && index < row.Count
            ? NullIfEmpty(row[index])
            : null;

    private static MediaStatus MapStatus(string? status, int? rating)
    {
        var value = status?.Trim().ToLowerInvariant() ?? "";
        var completed = value is "completed" or "complete" or "consumed" or "read" or "watched" or "finished" or "rated";
        if (completed)
        {
            return rating is null ? MediaStatus.Consumed : MediaStatus.Rated;
        }

        return MediaStatus.Planned;
    }

    private static MediaKind MapKind(string? kind) => kind?.Trim().ToLowerInvariant() switch
    {
        "film" or "movie" => MediaKind.Film,
        "series" or "show" or "tv" => MediaKind.Series,
        "book" or "novel" => MediaKind.Book,
        "game" => MediaKind.Game,
        "anime" => MediaKind.Anime,
        "manga" => MediaKind.Manga,
        _ => MediaKind.Other,
    };

    private static int? ParseRating(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rating) &&
        rating is >= 1 and <= 10
            ? rating
            : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(ch);
                }

                continue;
            }

            switch (ch)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    break;
                default:
                    field.Append(ch);
                    break;
            }
        }

        if (quoted)
        {
            throw new ImportException("The CSV has an unclosed quoted field.");
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
