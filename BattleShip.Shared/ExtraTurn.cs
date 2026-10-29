namespace BattleShip.Shared;

public sealed class ExtraTurn : PowerUp
{
    public ExtraTurn(GridPosition position) : base(position, "Extra turn")
    {
    }

    public override void Collect(GridModel grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        grid.AddExtraTurn();
    }

    public override PowerUpType GetPowerUpType() => PowerUpType.ExtraTurn;
}