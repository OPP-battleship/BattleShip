namespace BattleShip.Shared;

public sealed class SingleShotStrategy : IShotStrategy
{
    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        return [new ShotTarget(originX, originY)];
    }
}
