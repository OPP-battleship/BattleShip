namespace BattleShip.Shared;

public sealed class SmallObstacle : Obstacle
{
    public SmallObstacle(GridPosition position) : base(position)
    {
    }

    public override IEnumerable<GridPosition> GetOccupiedCells()
    {
        yield return Position;
    }
}