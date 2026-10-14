using System.Collections.Generic;

namespace BattleShip.Client;

internal sealed class PlacementCommandInvoker
{
    private readonly Stack<IPlacementCommand> _history = new();

    public bool CanUndo => _history.Count > 0;

    public bool TryExecute(IPlacementCommand command, out string? failureMessage)
    {
        if (!command.Execute(out failureMessage))
        {
            return false;
        }

        _history.Push(command);
        return true;
    }

    public bool TryUndo(out string? failureMessage)
    {
        if (_history.Count == 0)
        {
            failureMessage = "Nothing to undo.";
            return false;
        }

        var command = _history.Pop();
        command.Undo();
        failureMessage = null;
        return true;
    }

    public void Clear()
    {
        _history.Clear();
    }
}
