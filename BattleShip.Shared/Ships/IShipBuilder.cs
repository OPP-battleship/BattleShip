namespace BattleShip.Shared;

public interface IShipBuilder
{
    void Reset();
    void BuildHull();
    void BuildPointCost();
    void BuildUnlockedStrategy();
    Ship GetResult();
}
