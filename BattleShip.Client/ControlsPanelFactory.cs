using Avalonia.Controls;
using Avalonia.Layout;

namespace BattleShip.Client;

public sealed class ControlsPanelFactory : PanelFactory<ControlsPanel>
{
    protected override ControlsPanel CreatePanel()
    {
        var shotModeComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Width = 140,
            Items =
            {
                new ComboBoxItem { Content = "Single Shot" },
                new ComboBoxItem { Content = "Line Shot" },
                new ComboBoxItem { Content = "Spread Shot" },
            },
        };

        var lineOrientationComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Width = 140,
            Items =
            {
                new ComboBoxItem { Content = "Horizontal" },
                new ComboBoxItem { Content = "Vertical" },
            },
        };

        return new ControlsPanel(shotModeComboBox, lineOrientationComboBox)
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
            Children =
            {
                new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Shot Type", HorizontalAlignment = HorizontalAlignment.Center },
                        shotModeComboBox,
                    },
                },
                new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Line Direction", HorizontalAlignment = HorizontalAlignment.Center },
                        lineOrientationComboBox,
                    },
                },
            },
        };
    }
}
