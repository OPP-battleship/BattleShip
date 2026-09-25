namespace BattleShip.Shared;

public sealed class SpreadShotStrategy : IShotStrategy
{
    private readonly int _size;

    public SpreadShotStrategy(int size = 5)
    {
        _size = Math.Max(1, size);
    }

    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        var targets = new List<ShotTarget>(_size * _size);
        int startOffset = -(_size / 2);

        for (int yOffset = 0; yOffset < _size; yOffset++)
        {
            for (int xOffset = 0; xOffset < _size; xOffset++)
            {
                targets.Add(new ShotTarget(originX + startOffset + xOffset, originY + startOffset + yOffset));
            }
        }

        return targets;
    }
}
