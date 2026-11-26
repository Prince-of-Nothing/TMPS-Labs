namespace Watchlist.Behavioral.Import;

/// <summary>
/// Adapter target for external lists. AniList takes a username; MAL takes an
/// exported XML path or the XML text itself.
/// </summary>
public interface IWatchlistImporter
{
    string Name { get; }
    Task<IReadOnlyList<ImportedMedia>> ImportAsync(string input, CancellationToken cancellationToken = default);
}
