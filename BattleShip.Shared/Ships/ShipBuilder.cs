namespace BattleShip.Shared;

public abstract class ShipBuilder : IShipBuilder
{
    private Ship? _product;
    private bool _hasHull;
    private bool _hasPointCost;
    private bool _hasUnlockedStrategy;

    protected Ship Product => _product ?? throw new InvalidOperationException("Reset the builder before building a ship.");

    protected abstract Ship CreateProduct();

    public void Reset()
    {
        _product = CreateProduct();
        _hasHull = false;
        _hasPointCost = false;
        _hasUnlockedStrategy = false;
    }

    public abstract void BuildHull();
    public abstract void BuildPointCost();
    public abstract void BuildUnlockedStrategy();

    protected void SetHull(IEnumerable<GridPosition> shape)
    {
        Product.ConfigureShape(shape);
        _hasHull = true;
    }

    protected void SetPointCost(int pointCost)
    {
        Product.ConfigurePointCost(pointCost);
        _hasPointCost = true;
    }

    protected void SetUnlockedStrategy(ShotStrategyType? strategy)
    {
        Product.ConfigureUnlockedStrategy(strategy);
        _hasUnlockedStrategy = true;
    }

    public Ship GetResult()
    {
        if (!_hasHull || !_hasPointCost || !_hasUnlockedStrategy || !Product.IsConfigured)
        {
            throw new InvalidOperationException("Complete all ship-building steps before requesting the result.");
        }

        return Product;
    }
}
