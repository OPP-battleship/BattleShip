namespace BattleShip.Shared;

public sealed class Level1Factory : LevelFactory
{
    public override IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout) => boardLayout switch
    {
        BoardLayout.PlayerA =>
        [
            new SmallObstacle(new GridPosition(1, 0)),
            new SmallObstacle(new GridPosition(8, 3)),
            new SmallObstacle(new GridPosition(3, 6)),
            new SmallObstacle(new GridPosition(7, 9))
        ],
        BoardLayout.PlayerB =>
        [
            new SmallObstacle(new GridPosition(8, 0)),
            new SmallObstacle(new GridPosition(2, 3)),
            new SmallObstacle(new GridPosition(6, 6)),
            new SmallObstacle(new GridPosition(1, 9))
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    };

    public override IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout) => boardLayout switch
    {
        BoardLayout.PlayerA => [new ExtraTurn(new GridPosition(4, 4))],
        BoardLayout.PlayerB => [new ExtraTurn(new GridPosition(5, 5))],
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    };
}