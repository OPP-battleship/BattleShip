using System.Collections;

namespace BattleShip.Shared;

internal sealed class BoardPositionIterator(CellState[,] cells) : IPositionIterator, IEnumerator<GridPosition>
{
    private int _index = -1;

    public GridPosition Current =>
        _index >= 0 && _index < cells.Length
            ? new GridPosition(_index % cells.GetLength(0), _index / cells.GetLength(0))
            : throw new InvalidOperationException("The iterator is not positioned on a cell.");

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_index < cells.Length)
        {
            _index++;
        }

        return _index < cells.Length;
    }

    public void Reset() => _index = -1;

    public void Dispose() { }
}
