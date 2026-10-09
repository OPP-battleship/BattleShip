namespace BattleShip.Shared;

public abstract class BoardItem
{
    protected BoardItem(GridPosition position)
    {
        Position = position;
    }

    public GridPosition Position { get; }

    public virtual IEnumerable<GridPosition> GetOccupiedCells()
    {
        yield return Position;
    }
}