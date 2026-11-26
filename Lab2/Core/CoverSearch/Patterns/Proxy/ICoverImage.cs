namespace Watchlist.Core.CoverSearch.Images;

public sealed record CoverImageData(byte[] Bytes, string ContentType, string Source);

/// <summary>Subject interface shared by the real image and its proxy.</summary>
public interface ICoverImage
{
    Task<CoverImageData> GetAsync(CancellationToken cancellationToken = default);
}
