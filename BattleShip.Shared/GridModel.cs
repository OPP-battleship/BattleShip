namespace BattleShip.Shared;

public class GridModel : IPrototype<GridModel>
{
    public const int Size = 10;

    private readonly CellState[,] _cells = new CellState[Size, Size];
    private readonly int[,] _shipOwners = new int[Size, Size];
    private readonly List<PlacedShip> _placedShips = [];
    private readonly byte[,] _shots = new byte[Size, Size];
    private readonly PowerUp?[,] _powerUps = new PowerUp[Size, Size];
    private int _shotAreaUpCount;
    private int _extraTurnCount;
    private int _freeShotCount;

    private const byte ShotMiss = 1;
    private const byte ShotHit = 2;
    private const byte ShotObstacle = 3;
    private const byte ShotMissOnPowerUp = 4;

    private GridModel(GridModel source)
    {
        Array.Copy(source._cells, _cells, source._cells.Length);
        Array.Copy(source._shipOwners, _shipOwners, source._shipOwners.Length);
        Array.Copy(source._shots, _shots, source._shots.Length);
        Array.Copy(source._powerUps, _powerUps, source._powerUps.Length);
        _placedShips.AddRange(source._placedShips.Select(ship =>
            new PlacedShip(ShipCatalogue.Create(ship.Ship.Type), ship.RemainingCells)));
        _shotAreaUpCount = source._shotAreaUpCount;
        _extraTurnCount = source._extraTurnCount;
        _freeShotCount = source._freeShotCount;
    }

    public GridModel Clone() => new(this);

    public static GridModel FromFlatArray(
        IReadOnlyList<CellState> cells,
        IReadOnlyList<int>? shotCodes = null,
        IReadOnlyList<int>? powerUpCodes = null,
        int shotAreaUpCount = 0,
        int extraTurnCount = 0,
        int freeShotCount = 0)
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

        if (powerUpCodes is not null && powerUpCodes.Count != Size * Size)
        {
            throw new ArgumentException($"Expected {Size * Size} power-up codes.", nameof(powerUpCodes));
        }

        if (shotAreaUpCount < 0 || extraTurnCount < 0 || freeShotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shotAreaUpCount), "Power-up counts cannot be negative.");
        }

        var grid = new GridModel();
        grid._shotAreaUpCount = shotAreaUpCount;
        grid._extraTurnCount = extraTurnCount;
        grid._freeShotCount = freeShotCount;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                var cellState = cells[y * Size + x];
                grid._cells[x, y] = cellState;
                if (shotCodes is not null)
                {
                    grid._shots[x, y] = (byte)shotCodes[y * Size + x];
                }

                if (powerUpCodes is not null && cellState == CellState.PowerUp && powerUpCodes[y * Size + x] != 0)
                {
                    var type = (PowerUpType)powerUpCodes[y * Size + x];
                    if (!Enum.IsDefined(type))
                    {
                        throw new ArgumentOutOfRangeException(nameof(powerUpCodes), type, "Unknown power-up type.");
                    }

                    grid._powerUps[x, y] = PowerUpAdapter.Create(type, new GridPosition(x, y));
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
                _powerUps[position.X, position.Y] = powerUp;
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

            newShips.Add(new PlacedShip(ship, occupiedCells.Count));
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

    public bool TryAddPowerUps(IEnumerable<PowerUp> powerUps)
    {
        ArgumentNullException.ThrowIfNull(powerUps);

        var powerUpItems = powerUps.ToArray();
        var additions = new List<GridPosition>();
        var uniquePositions = new HashSet<GridPosition>();
        foreach (var powerUp in powerUpItems)
        {
            ArgumentNullException.ThrowIfNull(powerUp);
            foreach (var position in powerUp.GetOccupiedCells())
            {
                if (!IsInsideBounds(position.X, position.Y) ||
                    _cells[position.X, position.Y] != CellState.Empty ||
                    !uniquePositions.Add(position))
                {
                    return false;
                }

                additions.Add(position);
            }
        }

        foreach (var position in additions)
        {
            _cells[position.X, position.Y] = CellState.PowerUp;
        }

        foreach (var powerUp in powerUpItems)
        {
            foreach (var position in powerUp.GetOccupiedCells())
            {
                _powerUps[position.X, position.Y] = powerUp;
            }
        }

        return true;
    }

    public IReadOnlyList<ShotStrategyType> GetAvailableShotStrategies()
    {
        var strategies = new HashSet<ShotStrategyType> { ShotStrategyType.Single };
        if (_freeShotCount > 0)
        {
            return Enum.GetValues<ShotStrategyType>();
        }

        foreach (var ship in _placedShips)
        {
            if (ship.RemainingCells > 0 && ship.Ship.UnlockedStrategy is { } strategy)
            {
                strategies.Add(strategy);
            }
        }

        return strategies.OrderBy(strategy => strategy).ToArray();
    }

    public bool HasShotAreaUp => _shotAreaUpCount > 0;

    public bool HasFreeShot => _freeShotCount > 0;

    public void AddShotAreaUp() => _shotAreaUpCount++;

    public void AddExtraTurn() => _extraTurnCount++;

    public void AddFreeShot() => _freeShotCount++;

    public bool TryConsumeShotAreaUp()
    {
        if (_shotAreaUpCount == 0)
        {
            return false;
        }

        _shotAreaUpCount--;
        return true;
    }

    public bool TryConsumeFreeShot()
    {
        if (_freeShotCount == 0)
        {
            return false;
        }

        _freeShotCount--;
        return true;
    }

    public bool TryConsumeExtraTurn()
    {
        if (_extraTurnCount == 0)
        {
            return false;
        }

        _extraTurnCount--;
        return true;
    }

    public int[] ToPowerUpCodes()
    {
        var codes = new int[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (_powerUps[x, y] is { } powerUp)
                {
                    codes[y * Size + x] = (int)PowerUpAdapter.GetPowerUpType(powerUp);
                }
            }
        }

        return codes;
    }

    public (int ShotAreaUp, int ExtraTurn, int FreeShot) GetPowerUpCounts() =>
        (_shotAreaUpCount, _extraTurnCount, _freeShotCount);

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

    public int[] ToShotCodes()
    {
        var shotCodes = new int[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                shotCodes[y * Size + x] = _shots[x, y];
            }
        }

        return shotCodes;
    }

    public CellState[] ToUnfiredFlatArray()
    {
        var cells = new CellState[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                var state = _cells[x, y];
                cells[y * Size + x] = state == CellState.Fired ? CellState.Empty : state;
            }
        }

        return cells;
    }

    public IReadOnlyList<ShotMark> GetShotMarks()
    {
        var marks = new List<ShotMark>();

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                var shotCode = _shots[x, y];
                if (shotCode == 0)
                {
                    continue;
                }

                marks.Add(new ShotMark(x, y, shotCode == ShotHit, shotCode == ShotObstacle));
            }
        }

        return marks;
    }

    public bool TryMarkFired(int x, int y) => TryMarkFired(x, y, out _);

    public bool TryMarkFired(int x, int y, out PowerUp? collectedPowerUp)
    {
        collectedPowerUp = null;
        if (_cells[x, y] == CellState.Fired) return false;

        var previousState = _cells[x, y];
        if (previousState == CellState.PowerUp)
        {
            collectedPowerUp = _powerUps[x, y];
            _powerUps[x, y] = null;
        }

        if (previousState == CellState.Ship && _shipOwners[x, y] >= 0)
        {
            _placedShips[_shipOwners[x, y]].RemainingCells--;
        }

        _shots[x, y] = previousState switch
        {
            CellState.Ship => ShotHit,
            CellState.Obstacle => ShotObstacle,
            CellState.PowerUp => ShotMissOnPowerUp,
            _ => ShotMiss,
        };

        _cells[x, y] = CellState.Fired;
        return true;
    }

    private sealed class PlacedShip(Ship ship, int remainingCells)
    {
        public Ship Ship { get; } = ship;
        public int RemainingCells { get; set; } = remainingCells;
    }
}