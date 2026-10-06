using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using BattleShip.Shared;

namespace BattleShip.Client.Views;

public partial class MainWindow : Window
{
    private const string ServerUrl = "http://localhost:5058";

    private readonly ConnectionService _connection = new();
    private readonly PanelFactory _panelFactory = new();
    private readonly BattleShip.Client.Panel _controlsPanel;
    private readonly BattleShip.Client.Panel _shipPanel;
    private readonly IShotStrategy _singleShotStrategy = new SingleShotStrategy();
    private readonly IShotStrategy _horizontalLineShotStrategy = new LineShotStrategy(ShotOrientation.Horizontal);
    private readonly IShotStrategy _verticalLineShotStrategy = new LineShotStrategy(ShotOrientation.Vertical);
    private readonly IShotStrategy _spreadShotStrategy = new SpreadShotStrategy();

    private BattleShip.Client.Panel? _yourBoard;
    private BattleShip.Client.Panel? _enemyBoard;
    private BattleShip.Client.Panel? _placementBoard;
    private BattleShip.Client.Panel? _scoreboard;
    private readonly List<(int X, int Y)> _placementObstacles = [];
    private readonly List<ShipPlacement> _shipPlacements = [];

    private string? _myConnectionId;
    private string? _sessionId;
    private bool _isMyTurn;

    public MainWindow()
    {
        InitializeComponent();

        _controlsPanel = _panelFactory.Create(PanelFactory.Controls);
        ControlsGrid.Children.Add(_controlsPanel);

        _shipPanel = _panelFactory.Create(PanelFactory.Ships);
        _shipPanel.ReadyRequested += OnReadyRequested;
        ShipSelectionGrid.Children.Add(_shipPanel);

        _connection.PlacementStarted += OnPlacementStarted;
        _connection.GameStarted += OnGameStarted;
        _connection.ShotResultReceived += OnShotResultReceived;
        _connection.OpponentDisconnected += OnOpponentDisconnected;
    }

    private async void OnFindMatchClicked(object? sender, RoutedEventArgs e)
    {
        FindMatchButton.IsEnabled = false;
        StatusText.Text = "Connecting...";

        try
        {
            await _connection.ConnectAsync(ServerUrl);
            _myConnectionId = _connection.MyConnectionId;

            StatusText.Text = "Searching for an opponent...";
            await _connection.FindMatchAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
            FindMatchButton.IsEnabled = true;
        }
    }

    private void OnPlacementStarted(PlacementStartedMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _sessionId = msg.SessionId;
            _placementObstacles.Clear();
            for (int y = 0; y < GridModel.Size; y++)
            {
                for (int x = 0; x < GridModel.Size; x++)
                {
                    if (msg.Board[y * GridModel.Size + x] == CellState.Obstacle)
                    {
                        _placementObstacles.Add((x, y));
                    }
                }
            }

            _shipPlacements.Clear();
            _shipPanel.Reset();
            _placementBoard = _panelFactory.Create(PanelFactory.Board, BoardCellOwner.Placement,
                msg.Board, OnPlacementCellClicked);
            PlacementGrid.Children.Clear();
            PlacementGrid.Children.Add(_placementBoard);

            MenuPanel.IsVisible = false;
            GamePanel.IsVisible = false;
            PlacementPanel.IsVisible = true;
            PlacementStatusText.Text = "Select a ship and click its starting cell. Ships cannot touch.";
        });
    }

    private void OnGameStarted(GameStartedMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isMyTurn = msg.YouGoFirst;
            BuildGrids(msg.YourBoard);
            BuildScoreboard();
            PlacementPanel.IsVisible = false;
            GamePanel.IsVisible = true;
            UpdateTurnText();
        });
    }

    private void OnShotResultReceived(ShotResultMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            bool iShotThis = msg.ShooterConnectionId == _myConnectionId;

            var target = iShotThis ? _enemyBoard : _yourBoard;
            target?.ApplyShot(msg.X, msg.Y, msg.IsHit, msg.IsObstacle);

            if (msg.IsHit)
            {
                _scoreboard?.AddPoint(isYou: iShotThis);
            }

            _isMyTurn = msg.IsYourTurnNext;
            UpdateTurnText();
        });
    }

    private void OnOpponentDisconnected(OpponentDisconnectedMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = "Opponent disconnected.";
            PlacementPanel.IsVisible = false;
            GamePanel.IsVisible = false;
            MenuPanel.IsVisible = true;
            FindMatchButton.IsEnabled = true;
            _sessionId = null;
            _shipPlacements.Clear();
            _placementObstacles.Clear();
            _placementBoard = null;
            _shipPanel.Reset();
        });
    }

    private void OnPlacementCellClicked(int x, int y)
    {
        if (_placementBoard is null || !_shipPanel.CanPlaceSelectedShip)
        {
            return;
        }

        int length = _shipPanel.SelectedLength;
        var placement = new ShipPlacement(x, y, length, _shipPanel.SelectedOrientation);
        var tentativePlacements = new List<ShipPlacement>(_shipPlacements) { placement };
        var candidateGrid = new GridModel(_placementObstacles);

        if (!candidateGrid.TryPlaceShips(tentativePlacements))
        {
            PlacementStatusText.Text = "That ship does not fit there. Check the board edges, obstacles, and spacing.";
            return;
        }

        _shipPlacements.Add(placement);
        _placementBoard.ApplyShipPlacement(placement);
        _shipPanel.MarkShipPlaced(length);
        PlacementStatusText.Text = _shipPanel.HasCompleteFleet
            ? "Fleet complete. Select Ready to start when your opponent is ready."
            : $"Placed length-{length} ship. Select another ship to continue.";
    }

    private async void OnReadyRequested()
    {
        if (!_shipPanel.HasCompleteFleet || string.IsNullOrEmpty(_sessionId))
        {
            return;
        }

        _shipPanel.MarkReady();
        PlacementStatusText.Text = "Fleet submitted. Waiting for your opponent to finish placing ships...";

        try
        {
            await _connection.SubmitShipPlacementAsync(_sessionId, _shipPlacements);
        }
        catch (Exception ex)
        {
            _shipPanel.CancelReady();
            PlacementStatusText.Text = $"Could not submit fleet: {ex.Message}";
        }
    }

    private void BuildGrids(CellState[] board)
    {
        YourGrid.Children.Clear();
        EnemyGrid.Children.Clear();

        _yourBoard = _panelFactory.Create(PanelFactory.Board, BoardCellOwner.Player, board);
        YourGrid.Children.Add(_yourBoard);

        var hiddenEnemyBoard = new CellState[GridModel.Size * GridModel.Size];
        _enemyBoard = _panelFactory.Create(PanelFactory.Board, BoardCellOwner.Enemy, hiddenEnemyBoard,
            OnEnemyCellClicked);
        EnemyGrid.Children.Add(_enemyBoard);
    }

    private void BuildScoreboard()
    {
        _scoreboard = _panelFactory.Create(PanelFactory.Scoreboard);

        ScoreboardGrid.Children.Clear();
        ScoreboardGrid.Children.Add(_scoreboard);
    }
    private async void OnEnemyCellClicked(int x, int y)
    {
        if (!_isMyTurn) return;

        var shotStrategy = GetSelectedShotStrategy();
        await _connection.FireShotAsync(shotStrategy.GetTargets(x, y));
    }

    private IShotStrategy GetSelectedShotStrategy()
    {
        return _controlsPanel.ShotModeSelectedIndex switch
        {
            1 => _controlsPanel.LineOrientationSelectedIndex == 1 ? _verticalLineShotStrategy : _horizontalLineShotStrategy,
            2 => _spreadShotStrategy,
            _ => _singleShotStrategy
        };
    }

    private void UpdateTurnText()
    {
        TurnText.Text = _isMyTurn ? "Your turn" : "Opponent's turn";
    }
}