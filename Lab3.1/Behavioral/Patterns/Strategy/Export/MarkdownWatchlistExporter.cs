using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Export;

public sealed class MarkdownWatchlistExporter : IWatchlistExporter
{
    public string Name => "Markdown";
    public string FileExtension => ".md";

    public string Export(IReadOnlyCollection<MediaItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Watchlist export");
        sb.AppendLine();
        sb.AppendLine($"{items.Count} entries · {DateTimeOffset.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("| Title | Kind | Status | Rating |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var item in items.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"| {Inline(item.Title)} | {item.Kind} | {item.Status} | {(item.Rating is int rating ? $"{rating}/10" : "—")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Entries");
        foreach (var item in items.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.AppendLine($"### {item.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Kind:** {item.Kind}");
            sb.AppendLine($"- **Category:** {item.Category}");
            sb.AppendLine($"- **Tags:** {(item.Tags.Count == 0 ? "—" : string.Join(", ", item.Tags))}");
            sb.AppendLine($"- **Status:** {item.Status}{(item.Rating is int rating ? $" ({rating}/10)" : "")}");
            if (item.Progress is not null)
            {
                sb.AppendLine($"- **Progress:** {item.Progress}");
            }

            if (item.CoverPath is not null)
            {
                sb.AppendLine($"- **Cover:** [{item.CoverPath}](../{item.CoverPath})");
            }

            if (item.SourceUrl is not null)
            {
                sb.AppendLine($"- **Source:** {item.SourceUrl}");
            }

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                sb.AppendLine();
                sb.AppendLine(item.Notes.Trim());
            }
        }

        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string Inline(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
