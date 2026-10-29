namespace BattleShip.Shared;

public sealed class SpecialOperationsShipBuilder : ShipBuilder
{
    private bool _hasMovement;

    public void InstallMovement()
    {
        if (_hasMovement)
        {
            throw new InvalidOperationException("The movement capability is already installed.");
        }

        Product.ConfigureMovementCapability(new ShipMovementCapability(2, true));
        _hasMovement = true;
    }

    public override void BuildHullSection(IEnumerable<GridPosition> section)
    {
        var cells = MaterializeSection(section);
        if (cells.Count != 1 || cells.Any(cell => cell.X < 0 || cell.Y < 0))
        {
            throw new ArgumentException(
                "Each pressure-hull section must be one cell with non-negative offsets.", nameof(section));
        }

        if (BuiltHull.Count > 0 && !cells.Any(cell => BuiltHull.Any(existing =>
                Math.Abs(cell.X - existing.X) + Math.Abs(cell.Y - existing.Y) == 1)))
        {
            throw new InvalidOperationException("Each pressure-hull section must join the assembled hull.");
        }

        AppendSection(cells);
    }

    public override void SealHull()
    {
        if (!_hasMovement)
        {
            throw new InvalidOperationException("Install the movement capability before sealing the submarine hull.");
        }

        base.SealHull();
    }

    public override void Reset(Func<Ship> createProduct)
    {
        base.Reset(createProduct);
        _hasMovement = false;
    }
}
