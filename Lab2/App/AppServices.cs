using Microsoft.Maui.Storage;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.Storage;

namespace Lab2;

/// <summary>
/// Composition root for the Lab 2 client. Object creation stays in Core: the
/// store comes from the Factory Method and the cover pipeline from
/// <see cref="CoverServiceFactory"/>. Pages only see the repository and
/// <see cref="CoverService"/>.
/// </summary>
public sealed class AppServices
{
    private AppServices(WatchlistRepository repository, CoverService covers)
    {
        Repository = repository;
        Covers = covers;
    }

    public WatchlistRepository Repository { get; }
    public CoverService Covers { get; }
    public string RootPath => Repository.RootPath;

    public static AppServices Create()
    {
        var root = Environment.GetEnvironmentVariable("TMPS_LAB2_LIBRARY");
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(FileSystem.AppDataDirectory, "library");
        }

        var store = new MarkdownWatchlistStoreFactory().Create(root);
        var repository = new WatchlistRepository(store);

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TMPS-Watchlist-Lab2/1.0");

        return new AppServices(repository, CoverServiceFactory.Create(http, store.CoversPath));
    }
}
