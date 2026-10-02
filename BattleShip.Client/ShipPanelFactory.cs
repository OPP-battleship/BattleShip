using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.Generic;

namespace BattleShip.Client;

public sealed class ShipPanelFactory : PanelFactory<ShipPanel>
{
    protected override ShipPanel CreatePanel()
    {
        var shipButtons = new Dictionary<int, Button>
        {
            [4] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [3] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [2] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [1] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
        };

        var orientationComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Items =
            {
                new ComboBoxItem { Content = "Horizontal" },
                new ComboBoxItem { Content = "Vertical" },
            },
        };

        var readyButton = new Button
        {
            Content = "Ready",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        return new ShipPanel(shipButtons, orientationComboBox, readyButton)
        {
            Name = "ShipPanel",
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Select a ship" },
                shipButtons[4],
                shipButtons[3],
                shipButtons[2],
                shipButtons[1],
                new TextBlock { Text = "Orientation" },
                orientationComboBox,
                readyButton,
            },
        };
    }
}
