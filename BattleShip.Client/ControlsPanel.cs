using Avalonia.Controls;
using BattleShip.Shared;
using System.Collections.Generic;

namespace BattleShip.Client;

internal sealed class ControlsPanel : Panel
{
    private readonly ComboBox _shotModeComboBox;
    private readonly ComboBox _lineOrientationComboBox;

    internal ControlsPanel(Avalonia.Controls.Panel layout, ComboBox shotModeComboBox, ComboBox lineOrientationComboBox)
        : base(layout)
    {
        _shotModeComboBox = shotModeComboBox;
        _lineOrientationComboBox = lineOrientationComboBox;
    }

    public int ShotModeSelectedIndex => _shotModeComboBox.SelectedIndex;

    public int LineOrientationSelectedIndex => _lineOrientationComboBox.SelectedIndex;

    public ShotStrategyType SelectedShotStrategy => ShotModeSelectedIndex switch
    {
        1 => ShotStrategyType.Line,
        2 => ShotStrategyType.Spread,
        3 => ShotStrategyType.Ring,
        _ => ShotStrategyType.Single
    };

    public void SetAvailableShotStrategies(IEnumerable<ShotStrategyType> strategies)
    {
        var available = new HashSet<ShotStrategyType>(strategies) { ShotStrategyType.Single };
        var allStrategies = new[]
        {
            ShotStrategyType.Single,
            ShotStrategyType.Line,
            ShotStrategyType.Spread,
            ShotStrategyType.Ring
        };

        for (int index = 0; index < allStrategies.Length; index++)
        {
            if (_shotModeComboBox.Items[index] is ComboBoxItem item)
            {
                item.IsEnabled = available.Contains(allStrategies[index]);
            }
        }

        if (!available.Contains(SelectedShotStrategy))
        {
            _shotModeComboBox.SelectedIndex = 0;
        }
    }
}
