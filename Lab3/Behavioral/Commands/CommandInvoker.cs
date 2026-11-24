namespace Watchlist.Behavioral.Commands;

/// <summary>
/// Command invoker with undo/redo. Only successfully executed commands enter
/// the history.
/// </summary>
public sealed class CommandInvoker
{
    private readonly Stack<IWatchlistCommand> _undo = [];
    private readonly Stack<IWatchlistCommand> _redo = [];

    public event EventHandler? HistoryChanged;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoDescription => _undo.TryPeek(out var command) ? command.Description : null;
    public string? RedoDescription => _redo.TryPeek(out var command) ? command.Description : null;

    public void Execute(IWatchlistCommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!_undo.TryPeek(out var command))
        {
            return;
        }

        command.Undo();
        _undo.Pop();
        _redo.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!_redo.TryPeek(out var command))
        {
            return;
        }

        command.Execute();
        _redo.Pop();
        _undo.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
