namespace BattleShip.Shared;

public sealed class Level2Factory : LevelFactory
{
    public override IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout) => boardLayout switch
    {
        BoardLayout.PlayerA =>
        [
            new LargeObstacle(new GridPosition(1, 0)),
            new LargeObstacle(new GridPosition(7, 8))
        ],
        BoardLayout.PlayerB =>
        [
            new LargeObstacle(new GridPosition(7, 0)),
            new LargeObstacle(new GridPosition(1, 8))
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    };

    public override IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout) => boardLayout switch
    {
        BoardLayout.PlayerA =>
        [
            new ShotAreaUp(new GridPosition(5, 2)),
            new ShotAreaUp(new GridPosition(3, 6))
        ],
        BoardLayout.PlayerB =>
        [
            new ShotAreaUp(new GridPosition(3, 3)),
            new ShotAreaUp(new GridPosition(6, 6))
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    };
}