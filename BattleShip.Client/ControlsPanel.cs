using Avalonia.Controls;

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
}
