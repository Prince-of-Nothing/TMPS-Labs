namespace Watchlist.Core.Storage;

public sealed record VaultError(string FilePath, string Message);

public sealed record VaultLoadResult(
    IReadOnlyList<Domain.MediaItem> Items,
    IReadOnlyList<VaultError> Errors);
