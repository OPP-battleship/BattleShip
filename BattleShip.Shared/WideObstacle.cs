namespace BattleShip.Shared;

public enum WideObstacleOrientation
{
    Horizontal,
    Vertical
}

public sealed class WideObstacle : Obstacle
{
    public WideObstacle(GridPosition position, WideObstacleOrientation orientation) : base(position)
    {
        if (!Enum.IsDefined(orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
        }

        Orientation = orientation;
    }

    public WideObstacleOrientation Orientation { get; }

    public override IEnumerable<GridPosition> GetOccupiedCells()
    {
        for (int offset = 0; offset < 3; offset++)
        {
            yield return Orientation == WideObstacleOrientation.Horizontal
                ? new GridPosition(Position.X + offset, Position.Y)
                : new GridPosition(Position.X, Position.Y + offset);
        }
    }
}
