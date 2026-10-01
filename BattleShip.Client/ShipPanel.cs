using Avalonia.Controls;
using Avalonia.Media;
using BattleShip.Shared;
using System.Collections.Generic;
using System.Linq;

namespace BattleShip.Client;

public sealed class ShipPanel : StackPanel
{
    private readonly Dictionary<int, Button> _shipButtons;
    private readonly Dictionary<int, int> _remainingShips = new()
    {
        [4] = 1,
        [3] = 2,
        [2] = 3,
        [1] = 4,
    };
    private readonly ComboBox _orientationComboBox;
    private readonly Button _readyButton;
    private bool _isReady;

    internal ShipPanel(Dictionary<int, Button> shipButtons, ComboBox orientationComboBox, Button readyButton)
    {
        _shipButtons = shipButtons;
        _orientationComboBox = orientationComboBox;
        _readyButton = readyButton;

        foreach (var (length, button) in _shipButtons)
        {
            button.Click += (_, _) =>
            {
                SelectedLength = length;
                RefreshShipButtons();
            };
        }

        _readyButton.Click += (_, _) => ReadyRequested?.Invoke();
        SelectedLength = 4;
        RefreshShipButtons();
    }

    public event System.Action? ReadyRequested;

    public int SelectedLength { get; private set; }

    public ShipOrientation SelectedOrientation => _orientationComboBox.SelectedIndex == 1
        ? ShipOrientation.Vertical
        : ShipOrientation.Horizontal;

    public bool CanPlaceSelectedShip => !_isReady && _remainingShips[SelectedLength] > 0;

    public bool HasCompleteFleet => _remainingShips.Values.All(remaining => remaining == 0);

    public bool MarkShipPlaced(int length)
    {
        if (_isReady || !_remainingShips.TryGetValue(length, out int remaining) || remaining == 0)
        {
            return false;
        }

        _remainingShips[length] = remaining - 1;
        RefreshShipButtons();
        return true;
    }

    public void MarkReady()
    {
        if (!HasCompleteFleet)
        {
            return;
        }

        _isReady = true;
        RefreshShipButtons();
    }

    public void CancelReady()
    {
        _isReady = false;
        RefreshShipButtons();
    }

    public void Reset()
    {
        _remainingShips[4] = 1;
        _remainingShips[3] = 2;
        _remainingShips[2] = 3;
        _remainingShips[1] = 4;
        SelectedLength = 4;
        _isReady = false;
        RefreshShipButtons();
    }

    private void RefreshShipButtons()
    {
        foreach (var (length, button) in _shipButtons)
        {
            int remaining = _remainingShips[length];
            button.Content = $"{(SelectedLength == length ? "> " : string.Empty)}Length {length} ({remaining} left)";
            button.Background = SelectedLength == length ? Brushes.LightBlue : Brushes.Transparent;
            button.IsEnabled = remaining > 0 && !_isReady;
        }

        _readyButton.IsEnabled = HasCompleteFleet && !_isReady;
    }
}
