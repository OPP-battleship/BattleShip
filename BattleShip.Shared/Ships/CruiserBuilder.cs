namespace BattleShip.Shared;

public sealed class CruiserBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new Cruiser();

    public override void BuildHull() => SetHull([new(0, 0), new(0, 1), new(1, 1)]);

    public override void BuildPointCost() => SetPointCost(3);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(ShotStrategyType.Line);
}
