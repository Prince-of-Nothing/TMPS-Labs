using System.Text;

namespace Watchlist.Core.CoverSearch.Images;

/// <summary>
/// Proxy: keeps the ICoverImage contract, checks the local cache first, and
/// only then downloads. The client never talks to the real subject directly.
/// </summary>
public sealed class CachedCoverImageProxy(
    ICoverImage inner,
    string cachePath,
    long maxBytes = RemoteCoverImage.MaxBytes) : ICoverImage
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<CoverImageData> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(cachePath))
            {
                var cached = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                if (cached.Length > 0 && cached.Length <= maxBytes)
                {
                    return new CoverImageData(
                        cached,
                        CoverImageTypes.ContentTypeForPath(cachePath),
                        cachePath);
                }

                File.Delete(cachePath);
            }

            var downloaded = await inner.GetAsync(cancellationToken);
            if (downloaded.Bytes.Length == 0 || downloaded.Bytes.Length > maxBytes)
            {
                throw new CoverImageException("The downloaded cover has an invalid size.");
            }

            if (!CoverImageTypes.LooksLikeImage(downloaded.Bytes))
            {
                throw new CoverImageException("The downloaded file is not a recognised image.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            var temp = $"{cachePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temp, downloaded.Bytes, cancellationToken);
                File.Move(temp, cachePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }

            return new CoverImageData(
                downloaded.Bytes,
                downloaded.ContentType,
                cachePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public string CachePath => cachePath;

    public override string ToString() =>
        new StringBuilder("CachedCoverImageProxy(").Append(cachePath).Append(')').ToString();
}
