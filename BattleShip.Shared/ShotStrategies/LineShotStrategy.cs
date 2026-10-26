namespace BattleShip.Shared;

public sealed class LineShotStrategy : IShotStrategy
{
    private readonly ShotOrientation _orientation;

    public LineShotStrategy(ShotOrientation orientation)
    {
        _orientation = orientation;
    }

    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        var targets = new List<ShotTarget>(GridModel.Size);

        for (int index = 0; index < GridModel.Size; index++)
        {
            int x = _orientation == ShotOrientation.Horizontal ? index : originX;
            int y = _orientation == ShotOrientation.Vertical ? index : originY;
            targets.Add(new ShotTarget(x, y));
        }

        return targets;
    }
}
