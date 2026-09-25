namespace BattleShip.Shared;

public interface IShotStrategy
{
    IReadOnlyList<ShotTarget> GetTargets(int originX, int originY);
}
