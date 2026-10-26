namespace BattleShip.Shared;

public static class ShipPlacementRules
{
    public static int GetSpentPoints(IReadOnlyList<ShipPlacement>? placements)
    {
        if (placements is null)
        {
            return 0;
        }

        int points = 0;

        foreach (var placement in placements)
        {
            if (placement is null || !Enum.IsDefined(placement.Type))
            {
                return -1;
            }

            try
            {
                points += ShipCatalogue.Create(placement.Type).PointCost;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        return points;
    }

    public static bool HasCompleteFleet(IReadOnlyList<ShipPlacement>? placements) =>
        GetSpentPoints(placements) == ShipCatalogue.PointBudget;
}