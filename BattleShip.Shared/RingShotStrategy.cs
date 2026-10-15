namespace BattleShip.Shared;

public sealed class RingShotStrategy : IShotStrategy
{
    private const int Radius = 2;

    public IReadOnlyList<ShotTarget> GetTargets(int originX, int originY)
    {
        var targets = new List<ShotTarget>(16);

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
