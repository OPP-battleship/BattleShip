namespace BattleShip.Shared;

public sealed class Level2Factory : ILevelFactory
{
    private static readonly (int X, int Y)[] PlayerAObstacles =
    [
        (1, 0), (2, 0), (1, 1), (2, 1),
        (7, 8), (8, 8), (7, 9), (8, 9)
    ];

    private static readonly (int X, int Y)[] PlayerBObstacles =
    [
        (7, 0), (8, 0), (7, 1), (8, 1),
        (1, 8), (2, 8), (1, 9), (2, 9)
    ];

    public GridModel CreateGrid(BoardLayout boardLayout) => new(boardLayout switch
    {
        BoardLayout.PlayerA => PlayerAObstacles,
        BoardLayout.PlayerB => PlayerBObstacles,
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    });
}