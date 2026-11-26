using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Import;

public sealed record ImportPreview(
    IReadOnlyList<ImportedMedia> NewItems,
    IReadOnlyList<ImportedMedia> Duplicates);

/// <summary>
/// Maps imported rows to the app's model and removes duplicates by normalized
/// title + media kind. Nothing is written here.
/// </summary>
public sealed class ImportService
{
    public ImportPreview Preview(IEnumerable<ImportedMedia> imported, IEnumerable<MediaItem> existing)
    {
        var keys = new HashSet<string>(
            existing.Select(item => Key(item.Title, item.Kind)),
            StringComparer.OrdinalIgnoreCase);
        var newItems = new List<ImportedMedia>();
        var duplicates = new List<ImportedMedia>();

        foreach (var row in imported)
        {
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                continue;
            }

            if (keys.Add(Key(row.Title, row.Kind)))
            {
                newItems.Add(row);
            }
            else
            {
                duplicates.Add(row);
            }
        }

        return new ImportPreview(newItems, duplicates);
    }

    public IReadOnlyList<MediaItem> BuildItems(ImportPreview preview, Func<int> nextId)
    {
        var items = new List<MediaItem>();
        foreach (var row in preview.NewItems)
        {
            var status = row.Status;
            var rating = status == MediaStatus.Rated ? row.Rating : null;
            if (status == MediaStatus.Rated && rating is null)
            {
                status = MediaStatus.Consumed;
            }

            var item = new MediaItemBuilder()
                .WithTitle(row.Title)
                .WithKind(row.Kind)
                .WithCategory(row.Category)
                .WithTags(row.Tags)
                .WithStatus(status, rating)
                .WithProgress(row.Progress)
                .WithCover(null, row.CoverSourceUrl)
                .WithSource(row.SourceUrl, row.SourceStatus)
                .Build(nextId());
            items.Add(item);
        }

        return items;
    }

    public static string Key(string title, MediaKind kind) =>
        $"{NormalizeTitle(title)}|{kind}";

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastSpace = false;
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastSpace = false;
            }
            else if (!lastSpace && sb.Length > 0)
            {
                sb.Append(' ');
                lastSpace = true;
            }
        }

        return sb.ToString().Trim();
    }
}
