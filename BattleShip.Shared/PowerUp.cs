namespace BattleShip.Shared;

public abstract class PowerUp
{
    protected PowerUp(GridPosition position, string effect)
    {
        Position = position;
        Effect = effect;
    }

    public GridPosition Position { get; }
    public string Effect { get; }
}