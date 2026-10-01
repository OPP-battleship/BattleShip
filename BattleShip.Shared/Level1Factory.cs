namespace BattleShip.Shared;

public sealed class Level1Factory : ILevelFactory
{
    private static readonly (int X, int Y)[] PlayerAObstacles =
    [
        (1, 0), (8, 3), (3, 6), (7, 9)
    ];

    private static readonly (int X, int Y)[] PlayerBObstacles =
    [
        (8, 0), (2, 3), (6, 6), (1, 9)
    ];

    public GridModel CreateGrid(BoardLayout boardLayout) => new(boardLayout switch
    {
        BoardLayout.PlayerA => PlayerAObstacles,
        BoardLayout.PlayerB => PlayerBObstacles,
        _ => throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null)
    });
}