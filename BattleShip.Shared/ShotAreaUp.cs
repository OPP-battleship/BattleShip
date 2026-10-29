namespace BattleShip.Shared;

public sealed class ShotAreaUp : PowerUp
{
    public ShotAreaUp(GridPosition position) : base(position, "Shot area up")
    {
    }

    public override void Collect(GridModel grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        grid.AddShotAreaUp();
    }

    public override PowerUpType GetPowerUpType() => PowerUpType.ShotAreaUp;
}