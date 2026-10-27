namespace BattleShip.Shared;

public sealed class Level2Factory : LevelFactory
{
    public override string LevelId => "Level2";

    public override IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout) =>
        PlaceObstacles(
            boardLayout,
            position => new LargeObstacle(position),
            position => new LargeObstacle(position),
            position => new WideObstacle(position, (WideObstacleOrientation)Random.Shared.Next(2)));

    public override IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout, GridModel grid) =>
        PlacePowerUps(
            boardLayout,
            grid,
            position => new ShotAreaUp(position),
            position => new ShotAreaUp(position),
            position => new FreeShot(position));
}