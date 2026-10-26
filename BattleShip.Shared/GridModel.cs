namespace BattleShip.Shared;

public class GridModel : IPrototype<GridModel>
{
    public const int Size = 10;

    private readonly CellState[,] _cells = new CellState[Size, Size];
    private readonly int[,] _shipOwners = new int[Size, Size];
    private readonly List<PlacedShip> _placedShips = [];
    private readonly byte[,] _shots = new byte[Size, Size];

    private const byte ShotMiss = 1;
    private const byte ShotHit = 2;
    private const byte ShotObstacle = 3;
    private const byte ShotMissOnPowerUp = 4;

    private GridModel(GridModel source)
    {
        Array.Copy(source._cells, _cells, source._cells.Length);
        Array.Copy(source._shots, _shots, source._shots.Length);
    }

    public GridModel Clone() => new(this);

    public static GridModel FromFlatArray(IReadOnlyList<CellState> cells, IReadOnlyList<int>? shotCodes = null)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Count != Size * Size)
        {
            throw new ArgumentException($"Expected {Size * Size} cells.", nameof(cells));
        }

        if (shotCodes is not null && shotCodes.Count != Size * Size)
        {
            throw new ArgumentException($"Expected {Size * Size} shot codes.", nameof(shotCodes));
        }

        var grid = new GridModel();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                grid._cells[x, y] = cells[y * Size + x];
                if (shotCodes is not null)
                {
                    grid._shots[x, y] = (byte)shotCodes[y * Size + x];
                }
            }
        }

        return grid;
    }

    public GridModel() : this(Array.Empty<Obstacle>(), Array.Empty<PowerUp>())
    {
    }

    public GridModel(IEnumerable<(int X, int Y)> obstacles) : this(
        obstacles.Select(position => (Obstacle)new SmallObstacle(new GridPosition(position.X, position.Y))),
        Array.Empty<PowerUp>())
    {
    }

    public GridModel(IEnumerable<Obstacle> obstacles, IEnumerable<PowerUp> powerUps)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        ArgumentNullException.ThrowIfNull(powerUps);

        foreach (var obstacle in obstacles)
        {
            foreach (var position in obstacle.GetOccupiedCells())
            {
                PlaceInitialCell(position, CellState.Obstacle);
            }
        }

        foreach (var powerUp in powerUps)
        {
            foreach (var position in powerUp.GetOccupiedCells())
            {
                PlaceInitialCell(position, CellState.PowerUp);
            }
        }
    }

    private void PlaceInitialCell(GridPosition position, CellState cellState)
    {
        if (!IsInsideBounds(position.X, position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (_cells[position.X, position.Y] != CellState.Empty)
        {
            throw new ArgumentException($"Multiple board items use cell ({position.X}, {position.Y}).");
        }

        _cells[position.X, position.Y] = cellState;
    }

    public static bool IsInsideBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    public CellState GetCell(int x, int y) => _cells[x, y];

    public bool TryPlaceShips(IReadOnlyList<ShipPlacement>? placements)
    {
        if (placements is null || placements.Count == 0 || _placedShips.Count > 0)
        {
            return false;
        }

        var shipOwners = new int[Size, Size];
        var newShips = new List<PlacedShip>(placements.Count);
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                shipOwners[x, y] = -1;
                if (_cells[x, y] is CellState.Ship or CellState.Fired)
                {
                    return false;
                }
            }
        }

        for (int shipIndex = 0; shipIndex < placements.Count; shipIndex++)
        {
            var placement = placements[shipIndex];
            if (placement is null || !Enum.IsDefined(placement.Type))
            {
                return false;
            }

            var ship = ShipCatalogue.Create(placement.Type);
            var offsets = ship.GetOffsets(placement.Orientation);
            if (offsets.Count == 0)
            {
                return false;
            }

            var occupiedCells = new List<GridPosition>(offsets.Count);
            foreach (var offset in offsets)
            {
                int x = placement.X + offset.X;
                int y = placement.Y + offset.Y;
                if (!IsInsideBounds(x, y) || _cells[x, y] != CellState.Empty || shipOwners[x, y] != -1)
                {
                    return false;
                }

                for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
                {
                    for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
                    {
                        if (IsInsideBounds(neighborX, neighborY) &&
                            shipOwners[neighborX, neighborY] != -1 &&
                            shipOwners[neighborX, neighborY] != shipIndex)
                        {
                            return false;
                        }
                    }
                }

                shipOwners[x, y] = shipIndex;
                occupiedCells.Add(new GridPosition(x, y));
            }

            newShips.Add(new PlacedShip(ship, occupiedCells));
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (shipOwners[x, y] != -1)
                {
                    _cells[x, y] = CellState.Ship;
                    _shipOwners[x, y] = shipOwners[x, y];
                }
            }
        }

        _placedShips.AddRange(newShips);
        return true;
    }

    public IReadOnlyList<ShotStrategyType> GetAvailableShotStrategies()
    {
        var strategies = new HashSet<ShotStrategyType> { ShotStrategyType.Single };
        foreach (var ship in _placedShips)
        {
            if (ship.RemainingCells > 0 && ship.Ship.UnlockedStrategy is { } strategy)
            {
                strategies.Add(strategy);
            }
        }

        return strategies.OrderBy(strategy => strategy).ToArray();
    }

    public CellState[] ToFlatArray()
    {
        var cells = new CellState[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                cells[y * Size + x] = _cells[x, y];
            }
        }

        return cells;
    }

    public bool TryMarkFired(int x, int y)
    {
        if (_cells[x, y] == CellState.Fired) return false;
        _cells[x, y] = CellState.Fired;
        return true;
    }
}