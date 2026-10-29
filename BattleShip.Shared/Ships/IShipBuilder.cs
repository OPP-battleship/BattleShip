namespace BattleShip.Shared;

public interface IShipBuilder
{
    void Reset(Func<Ship> createProduct);
    void BuildHullSection(IEnumerable<GridPosition> section);
    void SealHull();
    void SetPointCost(int pointCost);
    void InstallUnlockedStrategy(ShotStrategyType strategy);
    Ship GetResult();
}
