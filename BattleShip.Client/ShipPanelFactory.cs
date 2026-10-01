using Avalonia.Controls;

namespace BattleShip.Client;

public sealed class ShipPanelFactory : IPanelFactory<Panel>
{
    public Panel Create() => new StackPanel { Name = "ShipPanel" };
}
