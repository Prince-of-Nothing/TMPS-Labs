namespace Watchlist.Behavioral.Commands;

public sealed record ImportProgress(int Processed, int Total, string Title);
