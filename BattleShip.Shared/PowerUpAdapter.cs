namespace BattleShip.Shared;

public sealed class PowerUpAdapter(PowerUp powerUp) : IPowerUpEffect
{
    public void ApplyTo(GridModel grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        switch (powerUp)
        {
            case ShotAreaUp:
                grid.AddShotAreaUp();
                break;
            case ExtraTurn:
                grid.AddExtraTurn();
                break;
            case FreeShot:
                grid.AddFreeShot();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(powerUp), powerUp.GetType(), "Unsupported power-up type.");
        }
    }

    public static PowerUpType GetPowerUpType(PowerUp powerUp) =>
        powerUp switch
        {
            ShotAreaUp => PowerUpType.ShotAreaUp,
            ExtraTurn => PowerUpType.ExtraTurn,
            FreeShot => PowerUpType.FreeShot,
            _ => throw new ArgumentOutOfRangeException(nameof(powerUp), powerUp.GetType(), "Unsupported power-up type.")
        };

    public static PowerUp Create(PowerUpType type, GridPosition position) =>
        type switch
        {
            PowerUpType.ShotAreaUp => new ShotAreaUp(position),
            PowerUpType.ExtraTurn => new ExtraTurn(position),
            PowerUpType.FreeShot => new FreeShot(position),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown power-up type.")
        };
}
