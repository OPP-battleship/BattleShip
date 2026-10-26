namespace BattleShip.Shared;

public sealed class AircraftCarrierBuilder : ShipBuilder
{
    protected override Ship CreateProduct() => new AircraftCarrier();

    public override void BuildHull() => SetHull(
        [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)]);

    public override void BuildPointCost() => SetPointCost(5);

    public override void BuildUnlockedStrategy() => SetUnlockedStrategy(null);
}
