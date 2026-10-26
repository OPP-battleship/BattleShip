namespace BattleShip.Shared;

public enum ShipOrientation
{
    Horizontal,
    Vertical,
}

public sealed record ShipPlacement(int X, int Y, ShipType Type, ShipOrientation Orientation);