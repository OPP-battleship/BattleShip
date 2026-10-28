using Avalonia.Media;
using System;
using Avalonia.Media;
using BattleShip.Shared;

namespace BattleShip.Client;

internal abstract class ShipView
{
    protected ShipView(IShipBackend backend)
    {
        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    protected IShipBackend Backend { get; }

    public ShipType Type => Backend.Type;
    public int PointCost => Backend.PointCost;

    public abstract string Caption { get; }

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
