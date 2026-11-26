using Microsoft.Maui.Storage;
using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Export;
using Watchlist.Behavioral.Import;
using Watchlist.Behavioral.Observers;
using Watchlist.Core.CoverSearch;
using Watchlist.Core.Storage;

namespace Lab3;

/// <summary>Composition root for the MAUI client.</summary>
public sealed class AppServices
{
    private readonly string _coversPath;

    private AppServices(
        WatchlistRepository repository,
        WatchlistEditor editor,
        WatchlistEvents events,
        CommandInvoker commands,
        CoverService covers,
        string coversPath,
        CoverProvider coverProvider,
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
        _coversPath = coversPath;
        CoverProvider = coverProvider;
        Exports = exports;
        Import = import;
        Importers = importers;
        Http = http;
    }

    public WatchlistRepository Repository { get; }
    public WatchlistEditor Editor { get; }
    public WatchlistEvents Events { get; }
    public CommandInvoker Commands { get; }
    public CoverService Covers { get; private set; }
    public CoverProvider CoverProvider { get; private set; }
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

        var providerName = Preferences.Default.Get("cover_provider", CoverProvider.Auto.ToString());
        var coverProvider = Enum.TryParse<CoverProvider>(providerName, ignoreCase: true, out var parsedProvider)
            ? parsedProvider
            : CoverProvider.Auto;

        // Lab 2's structural pipeline, created inside Core rather than here.
        var covers = CoverServiceFactory.Create(http, store.CoversPath, coverProvider);

        var importers = new IWatchlistImporter[]
        {
            new AniListImporter(http),
            new MalXmlImporter(),
            new CsvWatchlistImporter(),
        };

        return new AppServices(
            repository,
            editor,
            events,
            commands,
            covers,
            store.CoversPath,
            coverProvider,
            new ExportService(new CsvWatchlistExporter()),
            new ImportService(),
            importers,
            http);
    }

    public void SetCoverProvider(CoverProvider provider)
    {
        CoverProvider = provider;
        Covers = CoverServiceFactory.Create(Http, _coversPath, provider);
        Preferences.Default.Set("cover_provider", provider.ToString());
    }
}
