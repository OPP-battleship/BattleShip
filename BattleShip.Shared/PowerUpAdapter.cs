namespace BattleShip.Shared;

public sealed class PowerUpAdapter(PowerUp powerUp) : IPowerUpEffect
{
    public void ApplyTo(GridModel grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(powerUp);
        powerUp.Collect(grid);
    }
}
