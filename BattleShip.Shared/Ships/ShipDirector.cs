namespace BattleShip.Shared;

public sealed class ShipDirector
{
    public Ship ConstructShip(ShipType type, Func<Ship> createProduct) =>
        type switch
        {
            ShipType.AircraftCarrier => ConstructAircraftCarrier(createProduct),
            ShipType.Submarine => ConstructSubmarine(createProduct),
            ShipType.Cruiser => ConstructCruiser(createProduct),
            ShipType.PatrolBoat => ConstructPatrolBoat(createProduct),
            ShipType.Destroyer => ConstructDestroyer(createProduct),
            ShipType.Flagship => ConstructFlagship(createProduct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type.")
        };

    private static Ship ConstructAircraftCarrier(Func<Ship> createProduct)
    {
        var builder = StartSurfaceShip(createProduct);
        builder.BuildHullSection([new(0, 0), new(1, 0)]);
        builder.BuildHullSection([new(2, 0), new(3, 0), new(4, 0)]);
        builder.SealHull();
        builder.SetPointCost(5);
        return builder.GetResult();
    }

    private static Ship ConstructSubmarine(Func<Ship> createProduct)
    {
        var builder = new SpecialOperationsShipBuilder();
        builder.Reset(createProduct);
        builder.InstallPressureDrive();
        builder.SetPointCost(2);
        builder.BuildHullSection([new(0, 0)]);
        builder.BuildHullSection([new(1, 0)]);
        builder.SealHull();
        return builder.GetResult();
    }

    private static Ship ConstructCruiser(Func<Ship> createProduct)
    {
        var builder = StartSurfaceShip(createProduct);
        builder.BuildHullSection([new(0, 0), new(0, 1)]);
        builder.BuildHullSection([new(1, 1)]);
        builder.SealHull();
        builder.InstallUnlockedStrategy(ShotStrategyType.Line);
        builder.SetPointCost(3);
        return builder.GetResult();
    }

    private static Ship ConstructPatrolBoat(Func<Ship> createProduct)
    {
        var builder = StartSurfaceShip(createProduct);
        builder.BuildHullSection([new(0, 0)]);
        builder.SetPointCost(1);
        builder.SealHull();
        return builder.GetResult();
    }

    private static Ship ConstructDestroyer(Func<Ship> createProduct)
    {
        var builder = StartSurfaceShip(createProduct);
        builder.BuildHullSection([new(0, 0), new(1, 0), new(2, 0)]);
        builder.SealHull();
        builder.SetPointCost(3);
        builder.InstallUnlockedStrategy(ShotStrategyType.Spread);
        return builder.GetResult();
    }

    private static Ship ConstructFlagship(Func<Ship> createProduct)
    {
        var builder = StartSurfaceShip(createProduct);
        builder.BuildHullSection([new(0, 0), new(1, 0), new(2, 0)]);
        builder.BuildHullSection([new(1, 1)]);
        builder.SealHull();
        builder.InstallUnlockedStrategy(ShotStrategyType.Ring);
        builder.SetPointCost(5);
        return builder.GetResult();
    }

    private static SurfaceShipBuilder StartSurfaceShip(Func<Ship> createProduct)
    {
        ArgumentNullException.ThrowIfNull(createProduct);
        var builder = new SurfaceShipBuilder();
        builder.Reset(createProduct);
        return builder;
    }
}
