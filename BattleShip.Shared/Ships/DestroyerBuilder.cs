namespace BattleShip.Shared;

public sealed class DestroyerBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new Destroyer();

    public override void BuildHull() => SetHull([new(0, 0), new(1, 0), new(2, 0)]);

    public override void BuildPointCost() => SetPointCost(3);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(ShotStrategyType.Spread);
}
