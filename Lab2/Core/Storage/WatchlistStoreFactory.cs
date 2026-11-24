namespace Watchlist.Core.Storage;

/// <summary>Factory Method from Lab 1, reused for the storage variant.</summary>
public abstract class WatchlistStoreFactory
{
    public abstract IWatchlistStore Create(string rootPath);
}

public sealed class MarkdownWatchlistStoreFactory : WatchlistStoreFactory
{
    public override IWatchlistStore Create(string rootPath) => new MarkdownWatchlistStore(rootPath);
}
