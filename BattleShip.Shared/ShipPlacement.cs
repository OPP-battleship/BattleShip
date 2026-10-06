namespace BattleShip.Shared;

public enum ShipOrientation
{
    Horizontal,
    Vertical,
}

public sealed record ShipPlacement(int X, int Y, int Length, ShipOrientation Orientation);