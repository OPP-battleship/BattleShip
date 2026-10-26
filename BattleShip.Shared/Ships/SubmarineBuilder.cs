namespace BattleShip.Shared;

public sealed class SubmarineBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new Submarine();

    public override void BuildHull() => SetHull([new(0, 0), new(1, 0)]);

    public override void BuildPointCost() => SetPointCost(2);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(null);
}
