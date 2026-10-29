using BattleShip.Shared;

namespace BattleShip.Client;

/// <summary>Refined abstraction 1: ship on the selection button.</summary>
internal sealed class ShipButtonView(IShipBackend backend, int placedCount, bool isSelected) : ShipView(backend)
{
    public override string Label
    {
        get
        {
            string strategyName = Backend.UnlockedStrategy switch
            {
                ShotStrategyType.Line => "Line Shot",
                ShotStrategyType.Spread => "Spread Shot",
                ShotStrategyType.Ring => "Ring Shot",
                _ => "No special shot"
            };

            return $"{(isSelected ? "> " : string.Empty)}{ShipCatalogue.GetDisplayName(Backend.Type)} ({Backend.PointCost} AP; {strategyName}, placed {placedCount})";
        }
    }
}
