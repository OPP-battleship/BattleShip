using System.Collections;

namespace BattleShip.Shared;

internal sealed class PositionSetIterator(HashSet<GridPosition> positions) : IPositionIterator, IEnumerator<GridPosition>
{
    private readonly IEnumerator<GridPosition> _enumerator = positions.GetEnumerator();

    public GridPosition Current => _enumerator.Current;

    object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

    public bool MoveNext() => _enumerator.MoveNext();

    public void Reset() => _enumerator.Reset();

    public void Dispose() => _enumerator.Dispose();
}
