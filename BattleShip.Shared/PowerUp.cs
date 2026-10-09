namespace BattleShip.Shared;

public abstract class PowerUp : BoardItem
{
    protected PowerUp(GridPosition position, string effect) : base(position)
    {
        Effect = effect;
    }

    public string Effect { get; }
}