using BattleShip.Shared;

namespace BattleShip.Client;

internal sealed class PlaceShipCommand : IPlacementCommand
{
    private readonly IPlacementCommandReceiver _receiver;
    private readonly ShipPlacement _placement;

    public PlaceShipCommand(IPlacementCommandReceiver receiver, ShipPlacement placement)
    {
        _receiver = receiver;
        _placement = placement;
    }

    public bool Execute(out string? failureMessage)
    {
        if (!_receiver.CanPlaceShip(_placement, out failureMessage))
        {
            return false;
        }

        _receiver.CommitPlacement(_placement);
        failureMessage = null;
        return true;
    }

    public void Undo()
    {
        _receiver.RevertPlacement(_placement);
    }
}
