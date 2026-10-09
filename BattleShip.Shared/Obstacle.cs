namespace BattleShip.Shared;

public abstract class Obstacle : BoardItem
{
    protected Obstacle(GridPosition position) : base(position)
    {
    }
}