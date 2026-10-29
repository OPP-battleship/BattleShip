namespace BattleShip.Shared;

public sealed class SurfaceShipBuilder : ShipBuilder
{
    public override void BuildHullSection(IEnumerable<GridPosition> section)
    {
        var cells = MaterializeSection(section);
        if (BuiltHull.Count > 0 && !cells.Any(cell => BuiltHull.Any(existing =>
                Math.Abs(cell.X - existing.X) + Math.Abs(cell.Y - existing.Y) == 1)))
        {
            throw new InvalidOperationException("Each deck section must join the assembled surface hull.");
        }

        AppendSection(cells);
    }
}
