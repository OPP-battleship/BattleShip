namespace BattleShip.Shared;

public interface ILevelFactory
{
    GridModel CreateGrid(BoardLayout boardLayout);
}
