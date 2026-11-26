using Watchlist.Core.CoverSearch.Adapters;
using Watchlist.Core.CoverSearch.Decorators;
using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch;

/// <summary>
/// Assembles the structural cover pipeline in one place, so a client asks
/// for a <see cref="CoverService"/> and never sees the providers, adapters,
/// routing table or decorator order.
/// </summary>
public static class CoverServiceFactory
{
    public static CoverService Create(
        HttpClient http,
        string coversDirectory,
        CoverProvider provider = CoverProvider.Auto) =>
        new(CreateCatalog(http, provider), http, coversDirectory);

    /// <summary>
    /// Filtering -> Caching -> Routing -> provider Adapter. The filter sits
    /// outside the cache so cached results stay raw provider hits. A selected
    /// provider bypasses routing and is used for every kind.
    /// </summary>
    public static ICoverCatalog CreateCatalog(
        HttpClient http,
        CoverProvider provider = CoverProvider.Auto)
    {
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

        ICoverCatalog source = provider switch
        {
            CoverProvider.Wikimedia => wikimedia,
            CoverProvider.OpenLibrary => openLibrary,
            CoverProvider.TvMaze => tvMaze,
            _ => routing,
        };

        return new FilteringCoverCatalog(new CachingCoverCatalog(source));
    }
}
