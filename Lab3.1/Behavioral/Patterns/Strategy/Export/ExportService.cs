using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Export;

/// <summary>
/// Strategy context. The UI swaps the exporter at runtime; consumers only
/// know IWatchlistExporter.
/// </summary>
public sealed class ExportService(IWatchlistExporter initial)
{
    private IWatchlistExporter _exporter = initial;

    public IWatchlistExporter Exporter => _exporter;
    public string Name => _exporter.Name;
    public string FileExtension => _exporter.FileExtension;

    public void Use(IWatchlistExporter exporter) => _exporter = exporter;

    public string Export(IReadOnlyCollection<MediaItem> items) => _exporter.Export(items);
}
