namespace BattleShip.Shared;

internal interface IPositionIterator : IDisposable
{
    GridPosition Current { get; }
    bool MoveNext();
    void Reset();
}
