namespace BattleShip.Shared;

public static class ShipCatalogue
{
    public const int PointBudget = 30;
    private static readonly ShipDirector Director = new();

    public static IReadOnlyList<ShipType> AllTypes { get; } =
        Array.AsReadOnly(Enum.GetValues<ShipType>());

    public static Ship Create(ShipType type)
    {
        IShipBuilder builder = type switch
        {
            ShipType.AircraftCarrier => new AircraftCarrierBuilder(),
            ShipType.Submarine => new SubmarineBuilder(),
            ShipType.Cruiser => new CruiserBuilder(),
            ShipType.PatrolBoat => new PatrolBoatBuilder(),
            ShipType.Destroyer => new DestroyerBuilder(),
            ShipType.Flagship => new FlagshipBuilder(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type.")
        };

        return Director.ConstructShip(builder);
    }

    public static string GetDisplayName(ShipType type) => type switch
    {
        ShipType.AircraftCarrier => "Aircraft Carrier",
        ShipType.Submarine => "Submarine",
        ShipType.Cruiser => "Cruiser",
        ShipType.PatrolBoat => "Patrol Boat",
        ShipType.Destroyer => "Destroyer",
        ShipType.Flagship => "Flagship",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type.")
    };
}
