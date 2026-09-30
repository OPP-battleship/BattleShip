namespace BattleShip.Shared;

public class GridModel
{
    public const int Size = 10;
    private const int PrototypeShipCount = 25;

    private readonly CellState[,] _cells = new CellState[Size, Size];

    public GridModel() : this(Array.Empty<(int X, int Y)>())
    {
    }

    public GridModel(IEnumerable<(int X, int Y)> obstacles)
    {
        foreach (var (x, y) in obstacles)
        {
            if (!IsInsideBounds(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(obstacles));
            }

            _cells[x, y] = CellState.Obstacle;
        }

        int shipsPlaced = 0;
        for (int y = 0; y < Size && shipsPlaced < PrototypeShipCount; y++)
        {
            for (int x = 0; x < Size && shipsPlaced < PrototypeShipCount; x++)
            {
                if ((x + y) % 2 == 0 && (x / 2 + y / 2) % 2 == 0)
                {
                    int shipX = x;
                    while (shipX < Size && _cells[shipX, y] != CellState.Empty)
                    {
                        shipX++;
                    }

                    if (shipX < Size)
                    {
                        _cells[shipX, y] = CellState.Ship;
                        shipsPlaced++;
                    }
                }
            }
        }
    }

    public static bool IsInsideBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    public CellState GetCell(int x, int y) => _cells[x, y];

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