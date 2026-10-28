namespace BattleShip.Client;

using BattleShip.Shared;

/// <summary>Refined abstraction: ship as shown on the selection button.</summary>
internal sealed class ShipButtonView(IShipBackend backend) : ShipView(backend)
{
    public override string Caption => $"{Type} ({PointCost} pts, {Backend.GetOffsets(ShipOrientation.Horizontal).Count} cells)";
}
