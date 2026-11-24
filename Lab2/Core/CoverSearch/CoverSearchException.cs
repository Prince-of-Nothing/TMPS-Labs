namespace Watchlist.Core.CoverSearch;

public sealed class CoverSearchException(string message, Exception? innerException = null)
    : Exception(message, innerException);
