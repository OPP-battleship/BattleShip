using BattleShip.Shared;

namespace BattleShip.Client;

internal interface IPlacementCommandReceiver
{
    bool CanPlaceShip(ShipPlacement placement, out string failureMessage);
    void CommitPlacement(ShipPlacement placement);
    void RevertPlacement(ShipPlacement placement);
}
