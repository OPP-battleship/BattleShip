using System;
using Avalonia.Media;
using BattleShip.Shared;

namespace BattleShip.Client;

/// <summary>Bridge abstraction: UI side of a ship, delegating all game data to an <see cref="IShipBackend"/>.</summary>
internal abstract class ShipView
{
    protected ShipView(IShipBackend backend)
    {
        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    protected IShipBackend Backend { get; }

    public abstract string Label { get; }

    public IBrush Fill => Backend.Type switch
    {
        ShipType.AircraftCarrier => Brushes.SlateGray,
        ShipType.Submarine => Brushes.DarkSlateBlue,
        ShipType.Cruiser => Brushes.SteelBlue,
        ShipType.PatrolBoat => Brushes.CadetBlue,
        ShipType.Destroyer => Brushes.DarkCyan,
        ShipType.Flagship => Brushes.Goldenrod,
        _ => Brushes.Gray
    };
}
