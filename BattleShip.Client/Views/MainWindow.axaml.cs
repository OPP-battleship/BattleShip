using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using BattleShip.Client.Observers;
using BattleShip.Shared;

namespace BattleShip.Client.Views;

public partial class MainWindow : Window, IGameEventObserver, IPlacementCommandReceiver
{
    private const string ServerUrl = "http://localhost:5058";

    private readonly ConnectionService _connection = new();
    private readonly IGameEventObserver _gameEventObserver;
    private readonly ControlsPanel _controlsPanel;
    private readonly ShipPanel _shipPanel;
    private readonly IShotStrategy _singleShotStrategy = new SingleShotStrategy();
    private readonly IShotStrategy _horizontalLineShotStrategy = new LineShotStrategy(ShotOrientation.Horizontal);
    private readonly IShotStrategy _verticalLineShotStrategy = new LineShotStrategy(ShotOrientation.Vertical);
    private readonly IShotStrategy _spreadShotStrategy = new SpreadShotStrategy();
    private readonly IShotStrategy _ringShotStrategy = new RingShotStrategy();

    private BoardPanel? _yourBoard;
    private BoardPanel? _enemyBoard;
    private BoardPanel? _placementBoard;
    private ScoreboardPanel? _scoreboard;
    private readonly PlacementCommandInvoker _placementCommands = new();
    private readonly List<(int X, int Y)> _placementBlockedCells = [];
    private readonly List<ShipPlacement> _shipPlacements = [];
    private CellState[]? _placementBoardState;

    private string? _myConnectionId;
    private string? _sessionId;
    private bool _isMyTurn;

    public MainWindow()
    {
        InitializeComponent();

        _controlsPanel = (ControlsPanel)PanelFactory.Create(PanelFactory.Controls);
        ControlsGrid.Children.Add(_controlsPanel);

        _shipPanel = (ShipPanel)PanelFactory.Create(PanelFactory.Ships);
        _shipPanel.ReadyRequested += OnReadyRequested;
        _shipPanel.UndoRequested += OnUndoRequested;
        ShipSelectionGrid.Children.Add(_shipPanel);

        _gameEventObserver = new UiThreadGameEventObserverDecorator(
            new ExceptionHandlingGameEventObserverDecorator(
                new LoggingGameEventObserverDecorator(this)));
        _connection.Subscribe(_gameEventObserver);
        Closed += (_, _) => _connection.Unsubscribe(_gameEventObserver);
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

    public void OnPlacementStarted(PlacementStartedMessage msg)
    {
        _sessionId = msg.SessionId;
        _placementBoardState = (CellState[])msg.Board.Clone();
        _placementBlockedCells.Clear();
        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                if (msg.Board[y * GridModel.Size + x] is CellState.Obstacle or CellState.PowerUp)
                {
                    _placementBlockedCells.Add((x, y));
                }
            }
        }

        _shipPlacements.Clear();
        _shipPanel.Reset();
        _placementCommands.Clear();
        RefreshPlacementBoard();
        _shipPanel.SetUndoEnabled(false);

        MenuPanel.IsVisible = false;
        GamePanel.IsVisible = false;
        PlacementPanel.IsVisible = true;
        PlacementStatusText.Text = "Select a ship and click its starting cell. Ships cannot touch.";
    }

    public void OnGameStarted(GameStartedMessage msg)
    {
        _isMyTurn = msg.YouGoFirst;
        BuildGrids(msg.YourBoard);
        BuildScoreboard();
        PlacementPanel.IsVisible = false;
        GamePanel.IsVisible = true;
        UpdateTurnText();
    }

    public void OnGameResumed(GameResumedMessage msg)
    {
        _sessionId = msg.SessionId;
        _myConnectionId = _connection.MyConnectionId;
        _isMyTurn = msg.IsYourTurn;
        BuildGrids(msg.YourBoard);
        BuildScoreboard();

        foreach (var shot in msg.ShotsAtYou)
        {
            _yourBoard?.ApplyShot(shot.X, shot.Y, shot.IsHit, shot.IsObstacle);
            if (shot.IsHit)
            {
                _scoreboard?.AddPoint(isYou: false);
            }
        }

        foreach (var shot in msg.ShotsByYou)
        {
            _enemyBoard?.ApplyShot(shot.X, shot.Y, shot.IsHit, shot.IsObstacle);
            if (shot.IsHit)
            {
                _scoreboard?.AddPoint(isYou: true);
            }
        }

        MenuPanel.IsVisible = false;
        PlacementPanel.IsVisible = false;
        GamePanel.IsVisible = true;
        UpdateTurnText();
    }

    public void OnShotResult(ShotResultMessage msg)
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
    }

    public void OnOpponentDisconnected(OpponentDisconnectedMessage msg)
    {
        StatusText.Text = "Opponent disconnected.";
        PlacementPanel.IsVisible = false;
        GamePanel.IsVisible = false;
        MenuPanel.IsVisible = true;
        FindMatchButton.IsEnabled = true;
        _sessionId = null;
        _shipPlacements.Clear();
        _placementBlockedCells.Clear();
        _placementBoard = null;
        _placementBoardState = null;
        _placementCommands.Clear();
        _shipPanel.Reset();
        _shipPanel.SetUndoEnabled(false);
    }

    private void OnPlacementCellClicked(int x, int y)
    {
        if (_placementBoard is null || !_shipPanel.CanPlaceSelectedShip)
        {
            return;
        }

        int length = _shipPanel.SelectedLength;
        var placement = new ShipPlacement(x, y, length, _shipPanel.SelectedOrientation);
        var command = new PlaceShipCommand(this, placement);

        if (!_placementCommands.TryExecute(command, out var failureMessage))
        {
            PlacementStatusText.Text = failureMessage ?? "That ship does not fit there. Check the board edges, obstacles, and spacing.";
            return;
        }

        _shipPanel.SetUndoEnabled(_placementCommands.CanUndo);
    }

    private void OnUndoRequested()
    {
        if (!_placementCommands.TryUndo(out var failureMessage))
        {
            PlacementStatusText.Text = failureMessage ?? "Nothing to undo.";
            return;
        }

        _shipPanel.SetUndoEnabled(_placementCommands.CanUndo);
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

    public bool CanPlaceShip(ShipPlacement placement, out string failureMessage)
    {
        failureMessage = "";

        if (_placementBoardState is null)
        {
            failureMessage = "Placement board is not ready yet.";
            return false;
        }

        var tentativePlacements = new List<ShipPlacement>(_shipPlacements) { placement };
        var candidateGrid = new GridModel(_placementBlockedCells);

        if (!candidateGrid.TryPlaceShips(tentativePlacements))
        {
            failureMessage = "That ship does not fit there. Check the board edges, obstacles, and spacing.";
            return false;
        }

        return true;
    }

    public void CommitPlacement(ShipPlacement placement)
    {
        _shipPlacements.Add(placement);
        RefreshPlacementBoard();
        _shipPanel.MarkShipPlaced(placement.Length);
        PlacementStatusText.Text = _shipPanel.HasCompleteFleet
            ? "Fleet complete. Select Ready to start when your opponent is ready."
            : $"Placed length-{placement.Length} ship. Select another ship to continue.";
    }

    public void RevertPlacement(ShipPlacement placement)
    {
        if (!_shipPlacements.Remove(placement))
        {
            return;
        }

        RefreshPlacementBoard();
        _shipPanel.MarkShipRemoved(placement.Length);
        PlacementStatusText.Text = _shipPlacements.Count == 0
            ? "Select a ship and click its starting cell. Ships cannot touch."
            : "Last placement undone. Select another ship to continue.";
    }

    private void RefreshPlacementBoard()
    {
        if (_placementBoardState is null)
        {
            return;
        }

        _placementBoard = (BoardPanel)PanelFactory.Create(PanelFactory.Board, BoardCellOwner.Placement, _placementBoardState, OnPlacementCellClicked);
        foreach (var placement in _shipPlacements)
        {
            _placementBoard.ApplyShipPlacement(placement);
        }

        PlacementGrid.Children.Clear();
        PlacementGrid.Children.Add(_placementBoard);
    }

    private void BuildGrids(CellState[] board)
    {
        YourGrid.Children.Clear();
        EnemyGrid.Children.Clear();

        _yourBoard = (BattleShip.Client.BoardPanel)PanelFactory.Create(PanelFactory.Board, BoardCellOwner.Player, board);
        YourGrid.Children.Add(_yourBoard);

        var hiddenEnemyBoard = new CellState[GridModel.Size * GridModel.Size];
        _enemyBoard = (BattleShip.Client.BoardPanel)PanelFactory.Create(PanelFactory.Board, BoardCellOwner.Enemy, hiddenEnemyBoard,
            OnEnemyCellClicked);
        EnemyGrid.Children.Add(_enemyBoard);
    }

    private void BuildScoreboard()
    {
        _scoreboard = (BattleShip.Client.ScoreboardPanel)PanelFactory.Create(PanelFactory.Scoreboard);

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
            3 => _ringShotStrategy,
            _ => _singleShotStrategy
        };
    }

    private async void OnSaveGameClicked(object? sender, RoutedEventArgs e)
    {
        SaveGameButton.IsEnabled = false;
        try
        {
            var fileName = await _connection.SaveGameAsync();
            SaveStatusText.Text = $"Game saved to saves/{fileName}";
        }
        catch (Exception ex)
        {
            SaveStatusText.Text = $"Failed to save game: {ex.Message}";
        }
        finally
        {
            SaveGameButton.IsEnabled = true;
        }
    }

    private void UpdateTurnText()
    {
        TurnText.Text = _isMyTurn ? "Your turn" : "Opponent's turn";
    }
}
