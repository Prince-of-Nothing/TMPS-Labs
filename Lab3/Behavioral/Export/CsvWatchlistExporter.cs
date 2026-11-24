using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Export;

public sealed class CsvWatchlistExporter : IWatchlistExporter
{
    public string Name => "CSV";
    public string FileExtension => ".csv";

    public string Export(IReadOnlyCollection<MediaItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Kind,Category,Tags,Status,Rating,Progress,Cover,Source,SourceStatus,Updated,Notes");
        foreach (var item in items.OrderBy(item => item.Id))
        {
            sb.AppendLine(string.Join(',',
                item.Id.ToString(CultureInfo.InvariantCulture),
                Cell(item.Title),
                Cell(item.Kind.ToString()),
                Cell(item.Category),
                Cell(string.Join("; ", item.Tags)),
                Cell(item.Status.ToString()),
                item.Rating?.ToString(CultureInfo.InvariantCulture) ?? "",
                Cell(item.Progress ?? ""),
                Cell(item.CoverPath ?? ""),
                Cell(item.SourceUrl ?? ""),
                Cell(item.SourceStatus),
                Cell(item.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Cell(item.Notes)));
        }

        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string Cell(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
