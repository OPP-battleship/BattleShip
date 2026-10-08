namespace BattleShip.Shared;

public sealed class LargeObstacle : Obstacle
{
    public LargeObstacle(GridPosition position) : base(position)
    {
    }

    public override IEnumerable<GridPosition> GetOccupiedCells()
    {
        for (int y = Position.Y; y < Position.Y + 2; y++)
        {
            for (int x = Position.X; x < Position.X + 2; x++)
            {
                yield return new GridPosition(x, y);
            }
        }
    }
}