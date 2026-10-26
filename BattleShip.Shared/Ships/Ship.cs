namespace BattleShip.Shared;

public abstract class Ship
{
    public abstract ShipType Type { get; }
    public int PointCost { get; private set; }
    public IReadOnlyList<GridPosition> Shape { get; private set; } = Array.Empty<GridPosition>();
    public ShotStrategyType? UnlockedStrategy { get; private set; }

    private bool _hasPointCost;
    private bool _hasShape;
    private bool _hasUnlockedStrategy;

    protected Ship()
    {
    }

    internal void ConfigurePointCost(int pointCost)
    {
        if (pointCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointCost));
        }

        PointCost = pointCost;
        _hasPointCost = true;
    }

    internal void ConfigureShape(IEnumerable<GridPosition> shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        var cells = shape.ToArray();
        if (cells.Length == 0 || cells.Distinct().Count() != cells.Length ||
            cells.Any(position => position.X < 0 || position.Y < 0))
        {
            throw new ArgumentException("A ship shape must contain unique, non-negative cell offsets.", nameof(shape));
        }

        Shape = Array.AsReadOnly(cells);
        _hasShape = true;
    }

    internal void ConfigureUnlockedStrategy(ShotStrategyType? unlockedStrategy)
    {
        UnlockedStrategy = unlockedStrategy;
        _hasUnlockedStrategy = true;
    }

    public IReadOnlyList<GridPosition> GetOffsets(ShipOrientation orientation)
    {
        EnsureConfigured();
        return orientation switch
        {
            ShipOrientation.Horizontal => Shape,
            ShipOrientation.Vertical => RotateClockwise(),
            _ => Array.Empty<GridPosition>()
        };
    }

    internal bool IsConfigured => _hasPointCost && _hasShape && _hasUnlockedStrategy;

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("The ship must be fully built before use.");
        }
    }

    private IReadOnlyList<GridPosition> RotateClockwise()
    {
        int maxY = Shape.Max(position => position.Y);
        var rotated = Shape
            .Select(position => new GridPosition(maxY - position.Y, position.X))
            .ToArray();
        int minX = rotated.Min(position => position.X);
        int minY = rotated.Min(position => position.Y);

        return rotated
            .Select(position => new GridPosition(position.X - minX, position.Y - minY))
            .ToArray();
    }
}
