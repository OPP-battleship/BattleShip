using Avalonia.Controls;

namespace BattleShip.Client;

public sealed class ControlsPanel : StackPanel
{
    private readonly ComboBox _shotModeComboBox;
    private readonly ComboBox _lineOrientationComboBox;

    internal ControlsPanel(ComboBox shotModeComboBox, ComboBox lineOrientationComboBox)
    {
        _shotModeComboBox = shotModeComboBox;
        _lineOrientationComboBox = lineOrientationComboBox;
    }

    public int ShotModeSelectedIndex => _shotModeComboBox.SelectedIndex;

    public int LineOrientationSelectedIndex => _lineOrientationComboBox.SelectedIndex;
}
