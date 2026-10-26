namespace BattleShip.Shared;

public sealed class PatrolBoatBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new PatrolBoat();

    public override void BuildHull() => SetHull([new(0, 0)]);

    public override void BuildPointCost() => SetPointCost(1);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(null);
}
