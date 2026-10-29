namespace BattleShip.Shared;

public sealed record GridShipSnapshot(ShipType Type, GridPosition[] Cells, int RemainingCells);
