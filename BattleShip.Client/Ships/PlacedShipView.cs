using System.Collections.Generic;
using System.Linq;
using BattleShip.Shared;

namespace BattleShip.Client;

/// <summary>Refined abstraction: ship as drawn on the board.</summary>
internal sealed class PlacedShipView(IShipBackend backend, GridPosition origin, ShipOrientation orientation) : ShipView(backend)
{
    public override string Caption => Type.ToString();

    public IEnumerable<GridPosition> Cells => Backend.GetOffsets(orientation).Select(offset => new GridPosition(origin.X + offset.X, origin.Y + offset.Y));
}
