using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.Generic;

namespace BattleShip.Client;

public sealed class ShipPanelFactory : PanelFactory<Panel>
{
    protected override Panel CreatePanel()
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

        var layout = new StackPanel
        {
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

        return new Panel(layout, shipButtons, orientationComboBox, readyButton);
    }
}
