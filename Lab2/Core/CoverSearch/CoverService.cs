using Watchlist.Core.CoverSearch.Images;
using Watchlist.Core.Domain;

namespace Watchlist.Core.CoverSearch;

public sealed record CoverDownload(
    string RelativePath,
    long Bytes,
    CoverCandidate Candidate);

/// <summary>
/// Client-facing cover use case. Search goes through the decorated catalog;
/// saving always goes through the proxy, so a second selection is instant.
/// </summary>
public sealed class CoverService(
    ICoverCatalog catalog,
    HttpClient httpClient,
    string coversDirectory)
{
    public Task<IReadOnlyList<CoverCandidate>> SearchAsync(
        string query,
        MediaKind kind,
        CancellationToken cancellationToken = default) =>
        catalog.SearchAsync(query, kind, cancellationToken);

    public async Task<CoverDownload> SaveAsync(
        int entryId,
        CoverCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        // Provider thumbnails are enough for a card and keep the vault small;
        // the original URL is still kept as coverSource.
        var downloadUrl = string.IsNullOrWhiteSpace(candidate.ThumbnailUrl)
            ? candidate.ImageUrl
            : candidate.ThumbnailUrl;
        var extension = CoverImageTypes.ExtensionForUrl(downloadUrl);
        var path = Path.Combine(coversDirectory, $"{entryId:D4}{extension}");
        var proxy = new CachedCoverImageProxy(
            new RemoteCoverImage(httpClient, downloadUrl),
            path);
        var data = await proxy.GetAsync(cancellationToken);
        return new CoverDownload(
            $"covers/{Path.GetFileName(path)}",
            data.Bytes.LongLength,
            candidate);
    }
}
