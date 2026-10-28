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
    private readonly Dictionary<ShipType, Button> _shipButtons;
    private readonly ComboBox _shipOrientationComboBox;
    private readonly Button _readyButton;
    private readonly Button _undoButton;
    private readonly TextBlock _pointsText;
    private readonly Dictionary<ShipType, int> _shipCounts = [];
    private bool _undoEnabled;
    private bool _isReady;

    public event Action? ReadyRequested;
    public event Action? UndoRequested;

    internal ShipPanel(Avalonia.Controls.Panel layout, Dictionary<ShipType, Button> shipButtons,
        ComboBox orientationComboBox, Button readyButton, Button undoButton, TextBlock pointsText)
        : base(layout)
    {
        _shipButtons = shipButtons;
        _shipOrientationComboBox = orientationComboBox;
        _readyButton = readyButton;
        _undoButton = undoButton;
        _pointsText = pointsText;

        foreach (var (type, button) in _shipButtons)
        {
            button.Click += (_, _) =>
            {
                SelectedShipType = type;
                RefreshShipButtons();
            };
        }

        _readyButton.Click += (_, _) => ReadyRequested?.Invoke();
        _undoButton.Click += (_, _) => UndoRequested?.Invoke();
        SelectedShipType = ShipType.AircraftCarrier;
        RefreshShipButtons();
    }

    public ShipType SelectedShipType { get; private set; }

    public ShipOrientation SelectedOrientation => _shipOrientationComboBox.SelectedIndex == 1
        ? ShipOrientation.Vertical
        : ShipOrientation.Horizontal;

    public int PointsSpent => _shipCounts.Sum(entry => entry.Value * ShipCatalogue.Create(entry.Key).PointCost);

    public int PointsRemaining => ShipCatalogue.PointBudget - PointsSpent;

    public bool CanPlaceSelectedShip => CanPlaceShip(SelectedShipType);

    public bool HasCompleteFleet => PointsRemaining == 0;

    public bool CanPlaceShip(ShipType type) =>
        !_isReady && Enum.IsDefined(type) && ShipCatalogue.Create(type).PointCost <= PointsRemaining;

    public bool MarkShipPlaced(ShipType type)
    {
        if (!CanPlaceShip(type))
        {
            return false;
        }

        _shipCounts[type] = _shipCounts.GetValueOrDefault(type) + 1;
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

    public void MarkShipRemoved(ShipType type)
    {
        if (!_shipCounts.TryGetValue(type, out int count))
        {
            return;
        }

        if (count == 1)
        {
            _shipCounts.Remove(type);
        }
        else
        {
            _shipCounts[type] = count - 1;
        }

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
        _shipCounts.Clear();
        SelectedShipType = ShipType.AircraftCarrier;
        _undoEnabled = false;
        _isReady = false;
        RefreshShipButtons();
    }

    private void RefreshShipButtons()
    {
        foreach (var (type, button) in _shipButtons)
        {
            var ship = ShipCatalogue.Create(type);
            var view = new ShipButtonView(ship);
            int count = _shipCounts.GetValueOrDefault(type);
            bool isSelected = SelectedShipType == type;
            string strategyName = ship.UnlockedStrategy switch
            {
                ShotStrategyType.Line => "Line Shot",
                ShotStrategyType.Spread => "Spread Shot",
                ShotStrategyType.Ring => "Ring Shot",
                _ => "No special shot"
            };
            button.Content = $"{(isSelected ? "> " : string.Empty)}{ShipCatalogue.GetDisplayName(type)} ({view.PointCost} AP; {strategyName}, placed {count})";
            button.Background = view.Fill;
            button.IsEnabled = CanPlaceShip(type);
        }

        _pointsText.Text = $"Points: {PointsSpent}/{ShipCatalogue.PointBudget}";
        _readyButton.IsEnabled = HasCompleteFleet && !_isReady;
        _undoButton.IsEnabled = _undoEnabled && !_isReady;
    }
}
