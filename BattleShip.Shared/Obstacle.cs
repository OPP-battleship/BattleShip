namespace BattleShip.Shared;

public abstract class Obstacle
{
    protected Obstacle(GridPosition position)
    {
        Position = position;
    }

    public GridPosition Position { get; }

    public abstract IEnumerable<GridPosition> GetOccupiedCells();
}