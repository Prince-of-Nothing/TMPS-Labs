namespace Watchlist.Behavioral.Commands;

public interface IWatchlistCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
