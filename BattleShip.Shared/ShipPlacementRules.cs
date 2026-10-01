namespace BattleShip.Shared;

public static class ShipPlacementRules
{
    public static bool HasCompleteFleet(IReadOnlyList<ShipPlacement>? placements)
    {
        if (placements is null || placements.Count != 10)
        {
            return false;
        }

        int lengthFourCount = 0;
        int lengthThreeCount = 0;
        int lengthTwoCount = 0;
        int lengthOneCount = 0;

        foreach (var placement in placements)
        {
            if (placement is null)
            {
                return false;
            }

            switch (placement.Length)
            {
                case 4: lengthFourCount++; break;
                case 3: lengthThreeCount++; break;
                case 2: lengthTwoCount++; break;
                case 1: lengthOneCount++; break;
                default: return false;
            }
        }

        return lengthFourCount == 1 && lengthThreeCount == 2 &&
               lengthTwoCount == 3 && lengthOneCount == 4;
    }
}