namespace Watchlist.Behavioral.Import;

public sealed class ImportException(string message, Exception? innerException = null)
    : Exception(message, innerException);
