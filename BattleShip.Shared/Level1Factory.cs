namespace BattleShip.Shared;

public sealed class Level1Factory : LevelFactory
{
    public override string LevelId => "Level1";

    public override IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout) =>
        PlaceObstacles(
            boardLayout,
            position => new SmallObstacle(position),
            position => new SmallObstacle(position),
            position => new SmallObstacle(position),
            position => new SmallObstacle(position),
            position => new WideObstacle(position, (WideObstacleOrientation)Random.Shared.Next(2)));

    public override IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout, GridModel grid) =>
        PlacePowerUps(
            boardLayout,
            grid,
            position => new ExtraTurn(position),
            position => new FreeShot(position));
}