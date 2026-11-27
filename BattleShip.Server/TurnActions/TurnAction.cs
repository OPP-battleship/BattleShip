namespace BattleShip.Server.TurnActions;

/// <summary>
/// Template Method: every turn action follows the same fixed algorithm, only some steps differ.
/// </summary>
internal abstract class TurnAction : GameAction
{
    /// <summary>
    /// The template method. <c>sealed</c> prevents subclasses from changing the order of the steps.
    /// </summary>
    public sealed override bool Execute(GameSession session, string actorConnectionId)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (session.SyncRoot)
        {
            // 1. Invariant: the match must be running and it must be the actor's turn.
            if (!session.IsGameStarted || !session.IsPlayersTurn(actorConnectionId))
            {
                return false;
            }

            var opponentConnectionId = session.OpponentOf(actorConnectionId);
            var turn = new TurnContext(
                actorConnectionId,
                opponentConnectionId,
                session.GridOf(actorConnectionId),
                session.GridOf(opponentConnectionId));

            // 2. Primitive operation: action-specific request validation.
            if (!IsRequestValid(turn))
            {
                return false;
            }

            // 3. Primitive operation: action-specific effect on the board.
            if (!Perform(turn))
            {
                return false;
            }

            // 4. Hook: optional clean-up after a successful action.
            OnActionCompleted(turn);

            // 5. Invariant: hand the turn over (the hook decides whether the actor keeps it).
            session.PassTurnTo(KeepsTurn(turn) ? actorConnectionId : opponentConnectionId);
            return true;
        }
    }

    /// <summary>Primitive operation: checks the request itself.</summary>
    protected abstract bool IsRequestValid(TurnContext turn);

    /// <summary>Primitive operation: applies the action. Returns false if nothing happened.</summary>
    protected abstract bool Perform(TurnContext turn);

    /// <summary>Hook: does nothing by default.</summary>
    protected virtual void OnActionCompleted(TurnContext turn)
    {
    }

    /// <summary>Hook: by default the turn passes to the opponent.</summary>
    protected virtual bool KeepsTurn(TurnContext turn) => false;
}
