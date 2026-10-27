namespace BattleShip.Shared;

public sealed class ShotAreaUpShotStrategyAdapter(IShotStrategy strategy) : IShotStrategy
{
    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        return strategy switch
        {
            SingleShotStrategy => ExpandSingleShot(originX, originY),
            LineShotStrategy line => ExpandLine(line, originX, originY),
            SpreadShotStrategy spread => new SpreadShotStrategy(spread.Size + 1)
                .GetTargets(originX, originY),
            RingShotStrategy ring => new RingShotStrategy(ring.Radius + 1)
                .GetTargets(originX, originY),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy.GetType(),
                "Unsupported shot strategy.")
        };
    }

    private static IReadOnlyList<ShotTarget> ExpandSingleShot(int originX, int originY)
    {
        int horizontalOffset = originX + 1 < GridModel.Size ? 1 : -1;
        int verticalOffset = originY + 1 < GridModel.Size ? 1 : -1;
        return
        [
            new ShotTarget(originX, originY),
            new ShotTarget(originX + horizontalOffset, originY),
            new ShotTarget(originX, originY + verticalOffset),
            new ShotTarget(originX + horizontalOffset, originY + verticalOffset)
        ];
    }

    private static IReadOnlyList<ShotTarget> ExpandLine(
        LineShotStrategy line,
        int originX,
        int originY)
    {
        var targets = new HashSet<ShotTarget>(line.GetTargets(originX, originY));
        int offset = line.Orientation == ShotOrientation.Horizontal
            ? (originY + 1 < GridModel.Size ? 1 : -1)
            : (originX + 1 < GridModel.Size ? 1 : -1);

        foreach (var target in line.GetTargets(originX, originY))
        {
            targets.Add(line.Orientation == ShotOrientation.Horizontal
                ? new ShotTarget(target.X, target.Y + offset)
                : new ShotTarget(target.X + offset, target.Y));
        }

        return targets.ToArray();
    }
}
