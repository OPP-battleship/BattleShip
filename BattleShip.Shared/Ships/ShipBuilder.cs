namespace BattleShip.Shared;

public abstract class ShipBuilder : IShipBuilder
{
    private Ship? _product;
    private readonly List<GridPosition> _hull = [];
    private bool _hasHull;
    private bool _hasPointCost;

    protected Ship Product => _product ?? throw new InvalidOperationException("Reset the builder before building a ship.");
    protected IReadOnlyList<GridPosition> BuiltHull => _hull;

    public virtual void Reset(Func<Ship> createProduct)
    {
        ArgumentNullException.ThrowIfNull(createProduct);
        _product = createProduct() ?? throw new InvalidOperationException("The product factory returned null.");
        _hull.Clear();
        _hasHull = false;
        _hasPointCost = false;
    }

    public abstract void BuildHullSection(IEnumerable<GridPosition> section);

    public virtual void SealHull()
    {
        if (_hasHull)
        {
            throw new InvalidOperationException("The hull has already been sealed.");
        }

        if (_hull.Count == 0)
        {
            throw new InvalidOperationException("A ship must contain at least one hull cell.");
        }

        Product.ConfigureShape(_hull);
        _hasHull = true;
    }

    protected void AddHullSection(IEnumerable<GridPosition> section)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (_hasHull)
        {
            throw new InvalidOperationException("Cannot add a hull section after sealing the hull.");
        }

        _hull.AddRange(section);
    }

    public void SetPointCost(int pointCost)
    {
        Product.ConfigurePointCost(pointCost);
        _hasPointCost = true;
    }

    public void InstallUnlockedStrategy(ShotStrategyType strategy)
    {
        if (!Enum.IsDefined(strategy))
        {
            throw new ArgumentOutOfRangeException(nameof(strategy));
        }

        Product.ConfigureUnlockedStrategy(strategy);
    }

    public Ship GetResult()
    {
        if (!_hasHull || !_hasPointCost || !Product.IsConfigured)
        {
            throw new InvalidOperationException("Complete all ship-building steps before requesting the result.");
        }

        return Product;
    }

    protected static IReadOnlyList<GridPosition> MaterializeSection(IEnumerable<GridPosition> section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var cells = section.ToArray();
        if (cells.Length == 0)
        {
            throw new ArgumentException("A hull section cannot be empty.", nameof(section));
        }

        return cells;
    }

    protected void AppendSection(IEnumerable<GridPosition> section) =>
        AddHullSection(MaterializeSection(section));
}
