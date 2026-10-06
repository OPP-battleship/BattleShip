using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BattleShip.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShip.Client;

public sealed class Panel : UserControl
{
    private readonly Dictionary<int, Button>? _shipButtons;
    private readonly Dictionary<int, int>? _remainingShips;
    private readonly ComboBox? _shipOrientationComboBox;
    private readonly Button? _readyButton;
    private readonly ComboBox? _shotModeComboBox;
    private readonly ComboBox? _lineOrientationComboBox;
    private readonly Button[,]? _cells;
    private readonly bool[,]? _fired;
    private readonly TextBlock? _yourScoreText;
    private readonly TextBlock? _enemyScoreText;
    private int _yourScore;
    private int _enemyScore;
    private bool _isReady;

    internal Panel(Avalonia.Controls.Panel layout, Dictionary<int, Button> shipButtons,
        ComboBox orientationComboBox, Button readyButton)
    {
        Content = layout;
        _shipButtons = shipButtons;
        _remainingShips = new Dictionary<int, int>
        {
            [4] = 1,
            [3] = 2,
            [2] = 3,
            [1] = 4,
        };
        _shipOrientationComboBox = orientationComboBox;
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

    internal Panel(Avalonia.Controls.Panel layout, ComboBox shotModeComboBox, ComboBox lineOrientationComboBox)
    {
        Content = layout;
        _shotModeComboBox = shotModeComboBox;
        _lineOrientationComboBox = lineOrientationComboBox;
    }

    internal Panel(Avalonia.Controls.Panel layout, TextBlock yourScoreText, TextBlock enemyScoreText)
    {
        Content = layout;
        _yourScoreText = yourScoreText;
        _enemyScoreText = enemyScoreText;
        RefreshScore();
    }

    internal Panel(Avalonia.Controls.Panel layout, Button[,] cells)
    {
        Content = layout;
        _cells = cells;
        _fired = new bool[GridModel.Size, GridModel.Size];

        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                int capturedX = x, capturedY = y;
                _cells[x, y].Click += (_, _) => OnCellClicked(capturedX, capturedY);
            }
        }
    }

    public event Action? ReadyRequested;
    public event Action<int, int>? CellClicked;

    public int SelectedLength { get; private set; }

    public ShipOrientation SelectedOrientation => _shipOrientationComboBox?.SelectedIndex == 1
        ? ShipOrientation.Vertical
        : ShipOrientation.Horizontal;

    public bool CanPlaceSelectedShip => _remainingShips is not null && !_isReady && _remainingShips[SelectedLength] > 0;

    public bool HasCompleteFleet => _remainingShips is not null && _remainingShips.Values.All(remaining => remaining == 0);

    public int ShotModeSelectedIndex => _shotModeComboBox?.SelectedIndex
        ?? throw new InvalidOperationException("This panel does not contain shot controls.");

    public int LineOrientationSelectedIndex => _lineOrientationComboBox?.SelectedIndex
        ?? throw new InvalidOperationException("This panel does not contain shot controls.");

    public bool MarkShipPlaced(int length)
    {
        if (_remainingShips is null || _isReady || !_remainingShips.TryGetValue(length, out int remaining) || remaining == 0)
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
        if (_remainingShips is null)
        {
            return;
        }

        _remainingShips[4] = 1;
        _remainingShips[3] = 2;
        _remainingShips[2] = 3;
        _remainingShips[1] = 4;
        SelectedLength = 4;
        _isReady = false;
        RefreshShipButtons();
    }

    public void AddPoint(bool isYou)
    {
        if (_yourScoreText is null || _enemyScoreText is null)
        {
            throw new InvalidOperationException("This panel does not contain a scoreboard.");
        }

        if (isYou) _yourScore++;
        else _enemyScore++;
        RefreshScore();
    }

    public bool HasBeenFired(int x, int y) => _fired?[x, y]
        ?? throw new InvalidOperationException("This panel does not contain a game board.");

    public void ApplyShot(int x, int y, bool isHit, bool isObstacle)
    {
        if (_fired is null || _cells is null)
        {
            throw new InvalidOperationException("This panel does not contain a game board.");
        }

        _fired[x, y] = true;

        var cell = _cells[x, y];
        cell.Content = isHit || isObstacle ? "X" : "*";
        cell.Background = isObstacle
            ? Brushes.Gray
            : isHit
                ? Brushes.Red
                : Brushes.Gold;
    }

    public void ApplyShipPlacement(ShipPlacement placement)
    {
        if (_cells is null)
        {
            throw new InvalidOperationException("This panel does not contain a game board.");
        }

        int dx = placement.Orientation == ShipOrientation.Horizontal ? 1 : 0;
        int dy = placement.Orientation == ShipOrientation.Vertical ? 1 : 0;

        for (int offset = 0; offset < placement.Length; offset++)
        {
            var cell = _cells[placement.X + dx * offset, placement.Y + dy * offset];
            cell.Content = "X";
            cell.Background = Brushes.SteelBlue;
            cell.IsEnabled = false;
        }
    }

    private void RefreshShipButtons()
    {
        if (_shipButtons is null || _remainingShips is null || _readyButton is null)
        {
            return;
        }

        foreach (var (length, button) in _shipButtons)
        {
            int remaining = _remainingShips[length];
            button.Content = $"{(SelectedLength == length ? "> " : string.Empty)}Length {length} ({remaining} left)";
            button.Background = SelectedLength == length ? Brushes.LightBlue : Brushes.Transparent;
            button.IsEnabled = remaining > 0 && !_isReady;
        }

        _readyButton.IsEnabled = HasCompleteFleet && !_isReady;
    }

    private void RefreshScore()
    {
        if (_yourScoreText is null || _enemyScoreText is null)
        {
            return;
        }

        _yourScoreText.Text = $"You: {_yourScore}";
        _enemyScoreText.Text = $"Enemy: {_enemyScore}";
    }

    private void OnCellClicked(int x, int y)
    {
        if (_fired?[x, y] != false) return;
        CellClicked?.Invoke(x, y);
    }
}
