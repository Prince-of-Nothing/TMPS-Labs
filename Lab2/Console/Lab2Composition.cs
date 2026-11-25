using Watchlist.Core.CoverSearch;
using Watchlist.Core.Storage;

namespace Lab2;

/// <summary>
/// Composition root. Object creation stays in Core: the store comes from the
/// Factory Method and the cover pipeline from <see cref="CoverServiceFactory"/>.
/// The client only sees WatchlistRepository and CoverService.
/// </summary>
public static class Lab2Composition
{
    public static ConsoleApp Create(string root)
    {
        var store = new MarkdownWatchlistStoreFactory().Create(root);
        var repository = new WatchlistRepository(store);

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TMPS-Watchlist-Lab2/1.0");

        var covers = CoverServiceFactory.Create(http, store.CoversPath);

        return new ConsoleApp(repository, covers, store.RootPath);
    }
}
