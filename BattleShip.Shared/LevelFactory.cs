namespace BattleShip.Shared;

public abstract class LevelFactory
{
    public abstract IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout);
    public abstract IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout);
}