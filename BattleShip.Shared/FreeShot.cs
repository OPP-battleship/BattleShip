namespace BattleShip.Shared;

public sealed class FreeShot : PowerUp
{
    public FreeShot(GridPosition position) : base(position, "Free shot")
    {
    }

    public override void Collect(GridModel grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        grid.AddFreeShot();
    }

    public override PowerUpType GetPowerUpType() => PowerUpType.FreeShot;
}
