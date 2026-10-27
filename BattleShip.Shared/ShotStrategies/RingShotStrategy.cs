namespace BattleShip.Shared;

public sealed class RingShotStrategy : IShotStrategy
{
    public const int DefaultRadius = 2;

    public RingShotStrategy(int radius = DefaultRadius)
    {
        if (radius < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        Radius = radius;
    }

    public int Radius { get; }

    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        var targets = new List<ShotTarget>(Radius * 8);

        for (int yOffset = -Radius; yOffset <= Radius; yOffset++)
        {
            for (int xOffset = -Radius; xOffset <= Radius; xOffset++)
            {
                if (Math.Max(Math.Abs(xOffset), Math.Abs(yOffset)) != Radius)
                {
                    continue;
                }

                targets.Add(new ShotTarget(originX + xOffset, originY + yOffset));
            }
        }

        return targets;
    }
}
