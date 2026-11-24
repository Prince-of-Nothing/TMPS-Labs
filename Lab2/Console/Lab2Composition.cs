using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Adapters;
using Watchlist.Core.CoverSearch.Decorators;
using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Lab2;

/// <summary>
/// Composition root: chooses the concrete factory, adapters and decorators.
/// The rest of the client sees only ICoverCatalog and CoverService.
/// </summary>
public static class Lab2Composition
{
    public static ConsoleApp Create(string root)
    {
        var store = new MarkdownWatchlistStoreFactory().Create(root);
        var repository = new WatchlistRepository(store);

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TMPS-Watchlist-Lab2/1.0");

        var wikimedia = new WikimediaCoverAdapter(new WikimediaCommonsClient(http));
        var openLibrary = new OpenLibraryCoverAdapter(new OpenLibraryClient(http));
        var tvMaze = new TvMazeCoverAdapter(new TvMazeClient(http));

        var routing = new RoutingCoverCatalog(
            new Dictionary<MediaKind, ICoverCatalog>
            {
                [MediaKind.Book] = openLibrary,
                [MediaKind.Manga] = openLibrary,
                [MediaKind.Series] = tvMaze,
                [MediaKind.Anime] = tvMaze,
                [MediaKind.Film] = wikimedia,
                [MediaKind.Game] = wikimedia,
            },
            wikimedia);

        // Decorator chain: filter outside cache, so cached results stay raw.
        ICoverCatalog catalog = new FilteringCoverCatalog(new CachingCoverCatalog(routing));
        var covers = new CoverService(catalog, http, store.CoversPath);

        return new ConsoleApp(repository, covers, store.RootPath);
    }
}
