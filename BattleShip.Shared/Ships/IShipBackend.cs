namespace BattleShip.Shared;

/// <summary>Bridge implementor: the backend contract of a ship, free of any UI concerns.</summary>
public interface IShipBackend
{
    ShipType Type { get; }
    int PointCost { get; }
    ShotStrategyType? UnlockedStrategy { get; }
    IReadOnlyList<GridPosition> GetOffsets(ShipOrientation orientation);
}
