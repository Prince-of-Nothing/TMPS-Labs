using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Core.Storage;

/// <summary>
/// The index is derived data: a human opening the folder sees the whole
/// library without launching the app.
/// </summary>
public static class VaultIndexWriter
{
    public static string Write(
        IReadOnlyCollection<MediaItem> items,
        IReadOnlyDictionary<int, string> entryFileNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Watchlist");
        sb.AppendLine();
        sb.AppendLine("_Generated from the entry files. Edit those files, not this index._");
        sb.AppendLine();

        var planned = items.Count(item => item.Status == MediaStatus.Planned);
        var consumed = items.Count(item => item.Status == MediaStatus.Consumed);
        var rated = items.Count(item => item.Status == MediaStatus.Rated);
        sb.AppendLine($"**{items.Count} entries** · {planned} planned · {consumed} consumed · {rated} rated");
        sb.AppendLine();

        foreach (var status in new[] { MediaStatus.Planned, MediaStatus.Consumed, MediaStatus.Rated })
        {
            var group = items
                .Where(item => item.Status == status)
                .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            sb.AppendLine($"## {status} ({group.Length})");
            sb.AppendLine();
            if (group.Length == 0)
            {
                sb.AppendLine("_Nothing here._");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine("| Title | Kind | Category | Rating | Cover |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var item in group)
            {
                var file = entryFileNames[item.Id];
                var cover = item.CoverPath is null
                    ? "—"
                    : $"[cover]({item.CoverPath})";
                sb.Append("| [").Append(Inline(item.Title)).Append("](entries/").Append(Uri.EscapeDataString(file))
                    .Append(") | ").Append(item.KindLabel)
                    .Append(" | ").Append(Inline(item.Category))
                    .Append(" | ").Append(item.Rating is int rating ? $"{rating}/10" : "—")
                    .Append(" | ").Append(cover).AppendLine(" |");
            }

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Generated {DateTimeOffset.Now:yyyy-MM-dd HH:mm}.");
        return sb.ToString();
    }

    private static string Inline(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
