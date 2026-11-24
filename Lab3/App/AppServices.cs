using Microsoft.Maui.Storage;
using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Export;
using Watchlist.Behavioral.Import;
using Watchlist.Behavioral.Observers;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Adapters;
using Watchlist.Core.CoverSearch.Decorators;
using Watchlist.Core.CoverSearch.Providers;
using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Lab3;

/// <summary>Composition root for the MAUI client.</summary>
public sealed class AppServices
{
    private AppServices(
        WatchlistRepository repository,
        WatchlistEditor editor,
        WatchlistEvents events,
        CommandInvoker commands,
        CoverService covers,
        ExportService exports,
        ImportService import,
        IReadOnlyList<IWatchlistImporter> importers,
        HttpClient http)
    {
        Repository = repository;
        Editor = editor;
        Events = events;
        Commands = commands;
        Covers = covers;
        Exports = exports;
        Import = import;
        Importers = importers;
        Http = http;
    }

    public WatchlistRepository Repository { get; }
    public WatchlistEditor Editor { get; }
    public WatchlistEvents Events { get; }
    public CommandInvoker Commands { get; }
    public CoverService Covers { get; }
    public ExportService Exports { get; }
    public ImportService Import { get; }
    public IReadOnlyList<IWatchlistImporter> Importers { get; }
    public HttpClient Http { get; }
    public string RootPath => Repository.RootPath;

    public static AppServices Create(string? rootPath = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            rootPath = Environment.GetEnvironmentVariable("TMPS_LAB3_LIBRARY");
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            rootPath = Path.Combine(FileSystem.AppDataDirectory, "library");
        }

        var store = new MarkdownWatchlistStoreFactory().Create(rootPath);
        var repository = new WatchlistRepository(store);
        var events = new WatchlistEvents();
        var editor = new WatchlistEditor(repository, events);
        var commands = new CommandInvoker();

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TMPS-Watchlist-Lab3/1.0");

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
        ICoverCatalog catalog = new FilteringCoverCatalog(new CachingCoverCatalog(routing));
        var covers = new CoverService(catalog, http, store.CoversPath);

        var importers = new IWatchlistImporter[]
        {
            new AniListImporter(http),
            new MalXmlImporter(),
        };

        return new AppServices(
            repository,
            editor,
            events,
            commands,
            covers,
            new ExportService(new CsvWatchlistExporter()),
            new ImportService(),
            importers,
            http);
    }
}
