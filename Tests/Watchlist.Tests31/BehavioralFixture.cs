using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Observers;
using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Watchlist.Tests;

internal sealed class BehavioralFixture : IDisposable
{
    private readonly TempDirectory _temp = new();

    public BehavioralFixture()
    {
        Repository = new WatchlistRepository(new MarkdownWatchlistStore(_temp.Path));
        Events = new WatchlistEvents();
        Editor = new WatchlistEditor(Repository, Events);
        Invoker = new CommandInvoker();
    }

    public WatchlistRepository Repository { get; }
    public WatchlistEvents Events { get; }
    public WatchlistEditor Editor { get; }
    public CommandInvoker Invoker { get; }
    public string VaultPath => _temp.Path;

    public MediaItem NewItem(string title = "The Matrix", MediaKind kind = MediaKind.Film) =>
        new MediaItemBuilder()
            .WithTitle(title)
            .WithKind(kind)
            .WithCategory("Sci-Fi")
            .WithTags(["classic"])
            .WithNotes("Notes.")
            .Build(Editor.NextId);

    public MediaItem AddItem(string title = "The Matrix", MediaKind kind = MediaKind.Film)
    {
        var item = NewItem(title, kind);
        Editor.Add(item);
        return item;
    }

    public void Dispose() => _temp.Dispose();
}
