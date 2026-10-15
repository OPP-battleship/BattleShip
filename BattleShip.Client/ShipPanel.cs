using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BattleShip.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShip.Client;

internal sealed class ShipPanel : Panel
{
    private readonly Dictionary<int, Button> _shipButtons;
    private readonly Dictionary<int, int> _remainingShips = new()
    {
        [4] = 1,
        [3] = 2,
        [2] = 3,
        [1] = 4,
    };
    private readonly ComboBox _shipOrientationComboBox;
    private readonly Button _readyButton;
    private readonly Button _undoButton;
    private bool _undoEnabled;
    private bool _isReady;

    public event Action? ReadyRequested;
    public event Action? UndoRequested;

    internal ShipPanel(Avalonia.Controls.Panel layout, Dictionary<int, Button> shipButtons,
        ComboBox orientationComboBox, Button readyButton, Button undoButton)
        : base(layout)
    {
        _shipButtons = shipButtons;
        _shipOrientationComboBox = orientationComboBox;
        _readyButton = readyButton;
        _undoButton = undoButton;

        foreach (var (length, button) in _shipButtons)
        {
            button.Click += (_, _) =>
            {
                SelectedLength = length;
                RefreshShipButtons();
            };
        }

        _readyButton.Click += (_, _) => ReadyRequested?.Invoke();
        _undoButton.Click += (_, _) => UndoRequested?.Invoke();
        SelectedLength = 4;
        RefreshShipButtons();
    }

    public int SelectedLength { get; private set; }

    public ShipOrientation SelectedOrientation => _shipOrientationComboBox.SelectedIndex == 1
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

    public void MarkShipRemoved(int length)
    {
        if (!_remainingShips.ContainsKey(length))
        {
            return;
        }

        _remainingShips[length]++;
        _isReady = false;
        RefreshShipButtons();
    }

    public void SetUndoEnabled(bool enabled)
    {
        _undoEnabled = enabled;
        RefreshShipButtons();
    }

    public void Reset()
    {
        _remainingShips[4] = 1;
        _remainingShips[3] = 2;
        _remainingShips[2] = 3;
        _remainingShips[1] = 4;
        SelectedLength = 4;
        _undoEnabled = false;
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
        _undoButton.IsEnabled = _undoEnabled && !_isReady;
    }
}
