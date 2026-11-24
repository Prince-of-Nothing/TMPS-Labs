using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.Export;

/// <summary>Strategy: an interchangeable export algorithm.</summary>
public interface IWatchlistExporter
{
    string Name { get; }
    string FileExtension { get; }
    string Export(IReadOnlyCollection<MediaItem> items);
}
