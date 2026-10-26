namespace BattleShip.Shared;

public sealed class FlagshipBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new Flagship();

    public override void BuildHull() => SetHull([new(0, 0), new(1, 0), new(2, 0), new(1, 1)]);

    public override void BuildPointCost() => SetPointCost(5);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(ShotStrategyType.Ring);
}
