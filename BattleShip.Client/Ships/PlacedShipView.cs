using System.Collections.Generic;
using System.Linq;
using BattleShip.Shared;

namespace BattleShip.Client;

/// <summary>Refined abstraction 2: ship drawn on the board.</summary>
internal sealed class PlacedShipView(IShipBackend backend, GridPosition origin, ShipOrientation orientation) : ShipView(backend)
{
    public override string Label => ShipCatalogue.GetDisplayName(Backend.Type);

    /// <summary>Short text drawn inside each occupied cell.</summary>
    public string Marker => Label[..1];

    public IEnumerable<GridPosition> Cells => Backend.GetOffsets(orientation)
        .Select(offset => new GridPosition(origin.X + offset.X, origin.Y + offset.Y));
}
