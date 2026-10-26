namespace BattleShip.Shared;

public sealed class ShipDirector
{
    public Ship ConstructShip(IShipBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Reset();
        builder.BuildHull();
        builder.BuildPointCost();
        builder.BuildUnlockedStrategy();
        return builder.GetResult();
    }
}
