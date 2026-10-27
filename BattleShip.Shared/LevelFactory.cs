namespace BattleShip.Shared;

public abstract class LevelFactory
{
    public abstract string LevelId { get; }
    public abstract IReadOnlyList<Obstacle> GetObstacles(BoardLayout boardLayout);
    public abstract IReadOnlyList<PowerUp> GetPowerUps(BoardLayout boardLayout, GridModel grid);

    protected static IReadOnlyList<Obstacle> PlaceObstacles(
        BoardLayout boardLayout,
        params Func<GridPosition, Obstacle>[] obstacleFactories)
    {
        ValidateBoardLayout(boardLayout);
        return PlaceRandomly(obstacleFactories, obstacle => obstacle.GetOccupiedCells());
    }

    protected static IReadOnlyList<PowerUp> PlacePowerUps(
        BoardLayout boardLayout,
        GridModel grid,
        params Func<GridPosition, PowerUp>[] powerUpFactories)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ValidateBoardLayout(boardLayout);
        return PlaceRandomly(
            powerUpFactories,
            powerUp => powerUp.GetOccupiedCells(),
            position => grid.GetCell(position.X, position.Y) == CellState.Empty);
    }

    private static IReadOnlyList<T> PlaceRandomly<T>(
        IReadOnlyList<Func<GridPosition, T>> itemFactories,
        Func<T, IEnumerable<GridPosition>> getOccupiedCells,
        Func<GridPosition, bool>? isAvailable = null)
    {
        var occupiedPositions = new HashSet<GridPosition>();
        var placedItems = new List<T>(itemFactories.Count);
        var positions = (
            from y in Enumerable.Range(0, GridModel.Size)
            from x in Enumerable.Range(0, GridModel.Size)
            select new GridPosition(x, y)).ToArray();

        foreach (var itemFactory in itemFactories)
        {
            bool placed = false;
            foreach (var position in positions.OrderBy(_ => Random.Shared.Next()))
            {
                var item = itemFactory(position);
                var itemPositions = getOccupiedCells(item).ToArray();
                if (itemPositions.Length == 0 ||
                    itemPositions.Any(cell =>
                        !GridModel.IsInsideBounds(cell.X, cell.Y) ||
                        occupiedPositions.Contains(cell) ||
                        (isAvailable is not null && !isAvailable(cell))))
                {
                    continue;
                }

                foreach (var cell in itemPositions)
                {
                    occupiedPositions.Add(cell);
                }

                placedItems.Add(item);
                placed = true;
                break;
            }

            if (!placed)
            {
                throw new InvalidOperationException($"Could not place {typeof(T).Name} on the board.");
            }
        }

        return placedItems;
    }

    private static void ValidateBoardLayout(BoardLayout boardLayout)
    {
        if (!Enum.IsDefined(boardLayout))
        {
            throw new ArgumentOutOfRangeException(nameof(boardLayout), boardLayout, null);
        }
    }
}