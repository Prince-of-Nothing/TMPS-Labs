using Watchlist.Core.Domain;

namespace Watchlist.Core.Storage;

public interface IWatchlistStore
{
    string RootPath { get; }
    string CoversPath { get; }

    VaultLoadResult Load();
    void Save(MediaItem item);
    void Delete(int id);
    void WriteIndex(IReadOnlyCollection<MediaItem> items);
    string CoverFile(int id, string extension);
}
