using BattleShip.Shared;

namespace BattleShip.Server.TurnActions;

/// <summary>Concrete class: moves one of the actor's own ships instead of shooting.</summary>
internal sealed class MoveShipAction : TurnAction
{
    private readonly MoveShipRequest _request;

    public MoveShipAction(MoveShipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _request = request;
    }

    // A move must start on the board and go in exactly one straight direction.
    protected override bool IsRequestValid(TurnContext turn) =>
        GridModel.IsInsideBounds(_request.FromX, _request.FromY) &&
        (_request.DeltaX == 0) != (_request.DeltaY == 0);

    protected override bool Perform(TurnContext turn) =>
        turn.ActorGrid.TryMoveShip(_request.FromX, _request.FromY, _request.DeltaX, _request.DeltaY);

    // Hooks are not overridden: no clean-up, and the turn always passes to the opponent.
}
