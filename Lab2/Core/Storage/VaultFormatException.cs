namespace Watchlist.Core.Storage;

public sealed class VaultFormatException(string filePath, string message)
    : Exception($"{filePath}: {message}")
{
    public string FilePath { get; } = filePath;
}
