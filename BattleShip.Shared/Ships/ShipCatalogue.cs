using System.Collections.Concurrent;

namespace BattleShip.Shared;

public static class ShipCatalogue
{
    public const int PointBudget = 30;
    private static readonly ShipDirector Director = new();
    private static readonly ConcurrentDictionary<ShipType, Ship> Flyweights = new();

    public static IReadOnlyList<ShipType> AllTypes { get; } =
        Array.AsReadOnly(Enum.GetValues<ShipType>());

    public static Ship Create(ShipType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type.");
        }

        return Flyweights.GetOrAdd(type, CreateFlyweight);
    }

    private static Ship CreateFlyweight(ShipType type)
    {
        Func<Ship> createProduct = type switch
        {
            ShipType.AircraftCarrier => static () => new AircraftCarrier(),
            ShipType.Submarine => static () => new Submarine(),
            ShipType.Cruiser => static () => new Cruiser(),
            ShipType.PatrolBoat => static () => new PatrolBoat(),
            ShipType.Destroyer => static () => new Destroyer(),
            ShipType.Flagship => static () => new Flagship(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type.")
        };

        return Director.ConstructShip(type, createProduct);
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
