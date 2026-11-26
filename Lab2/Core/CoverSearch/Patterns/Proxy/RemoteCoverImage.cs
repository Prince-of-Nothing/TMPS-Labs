namespace Watchlist.Core.CoverSearch.Images;

/// <summary>Real subject: downloads bytes from the chosen provider URL.</summary>
public sealed class RemoteCoverImage(HttpClient httpClient, string imageUrl) : ICoverImage
{
    public const long MaxBytes = 12 * 1024 * 1024;

    public async Task<CoverImageData> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new CoverImageException("The cover URL is not an HTTP address.");
        }

        using var response = await httpClient.GetAsync(
            imageUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new CoverImageException($"The cover provider returned {(int)response.StatusCode}.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType) &&
            !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new CoverImageException($"The cover URL returned {mediaType}, not an image.");
        }

        if (response.Content.Headers.ContentLength is long declared && declared > MaxBytes)
        {
            throw new CoverImageException("The cover is larger than 12 MB.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > MaxBytes)
            {
                throw new CoverImageException("The cover is larger than 12 MB.");
            }

            memory.Write(buffer, 0, read);
        }

        var bytes = memory.ToArray();
        if (bytes.Length == 0)
        {
            throw new CoverImageException("The cover provider returned an empty file.");
        }

        return new CoverImageData(
            bytes,
            string.IsNullOrWhiteSpace(mediaType)
                ? CoverImageTypes.ContentTypeForPath(uri.AbsolutePath)
                : mediaType,
            imageUrl);
    }
}
