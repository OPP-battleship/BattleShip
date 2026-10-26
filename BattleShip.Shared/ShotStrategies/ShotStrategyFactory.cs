namespace BattleShip.Shared;

public static class ShotStrategyFactory
{
    public static IShotStrategy Create(ShotStrategyType type, ShotOrientation lineOrientation = ShotOrientation.Horizontal) =>
        type switch
        {
            ShotStrategyType.Single => new SingleShotStrategy(),
            ShotStrategyType.Line => new LineShotStrategy(lineOrientation),
            ShotStrategyType.Spread => new SpreadShotStrategy(),
            ShotStrategyType.Ring => new RingShotStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown shot strategy.")
        };
}
