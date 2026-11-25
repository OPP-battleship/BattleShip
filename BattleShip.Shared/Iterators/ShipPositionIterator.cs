using System.Collections;

namespace BattleShip.Shared;

internal sealed class ShipPositionIterator(
    IReadOnlyList<GridPosition> offsets,
    GridPosition origin) : IPositionIterator, IEnumerator<GridPosition>
{
    private int _index = -1;

    public GridPosition Current
    {
        get
        {
            if (_index < 0 || _index >= offsets.Count)
            {
                throw new InvalidOperationException("The iterator is not positioned on a ship cell.");
            }

            var offset = offsets[_index];
            return new GridPosition(origin.X + offset.X, origin.Y + offset.Y);
        }
    }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_index < offsets.Count)
        {
            _index++;
        }

        return _index < offsets.Count;
    }

    public void Reset() => _index = -1;

    public void Dispose() { }
}
