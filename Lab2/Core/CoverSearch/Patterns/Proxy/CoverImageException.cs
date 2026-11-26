namespace Watchlist.Core.CoverSearch.Images;

public sealed class CoverImageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
