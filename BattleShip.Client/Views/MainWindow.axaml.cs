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
    private string? _latestPowerUpNotice;
    private GridPosition? _moveOrigin;
    private bool _isMyTurn;
    private bool _isMoveMode;
    private bool _movePending;
    private bool _hasSubmarine;

    public MainWindow()
    {
        InitializeComponent();

        _controlsPanel = (ControlsPanel)PanelFactory.Create(PanelFactory.Controls);
        _controlsPanel.MoveRequested += OnMoveRequested;
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
        PlacementStatusText.Text = $"Select ships and click their starting cell. Spend all {ShipCatalogue.PointBudget} points. Ships cannot touch.";
    }

    public void OnPlacementCompleted(PlacementCompletedMessage msg)
    {
        if (msg.SessionId != _sessionId || _placementBoardState is null)
        {
            return;
        }

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

        RefreshPlacementBoard();
        PlacementStatusText.Text = "Ships placed. Power-ups have appeared. Waiting for your opponent...";
    }

    public void OnGameStarted(GameStartedMessage msg)
    {
        _latestPowerUpNotice = null;
        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
        _hasSubmarine = msg.HasSubmarine;
        _isMyTurn = msg.YouGoFirst;
        _controlsPanel.SetAvailableShotStrategies(msg.AvailableShotStrategies);
        BuildGrids(msg.YourBoard, msg.SubmarineCells ?? []);
        MoveHintText.IsVisible = false;
        MoveHintText.Text = "Click a cell marked S to select your submarine, then a straight-line destination up to 2 cells away. Moving uses your turn.";
        BuildScoreboard();
        PlacementPanel.IsVisible = false;
        GamePanel.IsVisible = true;
        UpdateTurnText();
    }

    public void OnGameResumed(GameResumedMessage msg)
    {
        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
        _hasSubmarine = msg.HasSubmarine;
        _sessionId = msg.SessionId;
        _myConnectionId = _connection.MyConnectionId;
        _isMyTurn = msg.IsYourTurn;
        BuildGrids(msg.YourBoard, msg.SubmarineCells ?? []);
        MoveHintText.IsVisible = false;
        MoveHintText.Text = "Click a cell marked S to select your submarine, then a straight-line destination up to 2 cells away. Moving uses your turn.";
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
        _controlsPanel.SetAvailableShotStrategies(msg.AvailableShotStrategies);
        bool iShotThis = msg.ShooterConnectionId == _myConnectionId;
        if (iShotThis && msg.CollectedPowerUp is { } powerUp)
        {
            _latestPowerUpNotice = powerUp switch
            {
                PowerUpType.ShotAreaUp => "Shot Area Up collected; your next shot is enlarged.",
                PowerUpType.ExtraTurn => "Extra Turn collected.",
                PowerUpType.FreeShot => "Free Shot collected; any strategy is available for your next shot.",
                _ => "Power-up collected."
            };
        }

        var target = iShotThis ? _enemyBoard : _yourBoard;
        target?.ApplyShot(msg.X, msg.Y, msg.IsHit, msg.IsObstacle);

        if (msg.IsHit)
        {
            _scoreboard?.AddPoint(isYou: iShotThis);
        }

        _isMyTurn = msg.IsYourTurnNext;
        if (!_isMyTurn)
        {
            CancelMoveMode();
        }

        UpdateTurnText();
    }

    public void OnShipMoved(ShipMovedMessage msg)
    {
        if (msg.YourBoard is { } board)
        {
            BuildYourBoard(board, msg.SubmarineCells ?? []);
            foreach (var shot in msg.YourShotsAtYou ?? [])
            {
                _yourBoard?.ApplyShot(shot.X, shot.Y, shot.IsHit, shot.IsObstacle);
            }
        }

        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
        MoveHintText.IsVisible = false;
        _isMyTurn = msg.IsYourTurnNext;
        MoveHintText.Text = "Submarine moved. The move used your turn.";
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
        MoveHintText.IsVisible = false;
        _hasSubmarine = false;
        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
        _controlsPanel.SetMoveButton(false, false, false);
        _shipPlacements.Clear();
        _placementBlockedCells.Clear();
        _placementBoard = null;
        _placementBoardState = null;
        _placementCommands.Clear();
        _shipPanel.Reset();
        _shipPanel.SetUndoEnabled(false);
        _latestPowerUpNotice = null;
        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
    }

    private void OnPlacementCellClicked(int x, int y)
    {
        if (_placementBoard is null || !_shipPanel.CanPlaceSelectedShip)
        {
            return;
        }

        var placement = new ShipPlacement(x, y, _shipPanel.SelectedShipType, _shipPanel.SelectedOrientation);
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
        if (ShipPlacementRules.GetSpentPoints(tentativePlacements) > ShipCatalogue.PointBudget)
        {
            failureMessage = "That ship costs more points than you have remaining.";
            return false;
        }

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
        _shipPanel.MarkShipPlaced(placement.Type);
        PlacementStatusText.Text = _shipPanel.HasCompleteFleet
            ? "All points spent. Select Ready to start when your opponent is ready."
            : $"Placed {ShipCatalogue.GetDisplayName(placement.Type)}. {_shipPanel.PointsRemaining} points remaining.";
    }

    public void RevertPlacement(ShipPlacement placement)
    {
        if (!_shipPlacements.Remove(placement))
        {
            return;
        }

        RefreshPlacementBoard();
        _shipPanel.MarkShipRemoved(placement.Type);
        PlacementStatusText.Text = _shipPlacements.Count == 0
            ? $"Select ships and spend all {ShipCatalogue.PointBudget} points. Ships cannot touch."
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

    private void BuildGrids(CellState[] board, IReadOnlyList<GridPosition> submarineCells)
    {
        YourGrid.Children.Clear();
        EnemyGrid.Children.Clear();

        BuildYourBoard(board, submarineCells);

        var hiddenEnemyBoard = new CellState[GridModel.Size * GridModel.Size];
        _enemyBoard = (BattleShip.Client.BoardPanel)PanelFactory.Create(PanelFactory.Board, BoardCellOwner.Enemy, hiddenEnemyBoard,
            OnEnemyCellClicked);
        EnemyGrid.Children.Add(_enemyBoard);
    }

    private void BuildYourBoard(CellState[] board, IReadOnlyList<GridPosition> submarineCells)
    {
        YourGrid.Children.Clear();
        _yourBoard = (BattleShip.Client.BoardPanel)PanelFactory.Create(
            PanelFactory.Board, BoardCellOwner.Player, board, OnYourCellClicked);
        _yourBoard.SetSubmarineCells(submarineCells);
        YourGrid.Children.Add(_yourBoard);
    }

    private void BuildScoreboard()
    {
        _scoreboard = (BattleShip.Client.ScoreboardPanel)PanelFactory.Create(PanelFactory.Scoreboard);

        ScoreboardGrid.Children.Clear();
        ScoreboardGrid.Children.Add(_scoreboard);
    }
    private async void OnEnemyCellClicked(int x, int y)
    {
        if (!_isMyTurn || _isMoveMode || _movePending) return;

        var lineOrientation = _controlsPanel.LineOrientationSelectedIndex == 1
            ? ShotOrientation.Vertical
            : ShotOrientation.Horizontal;
        await _connection.FireShotAsync(_controlsPanel.SelectedShotStrategy, x, y, lineOrientation);
    }

    private void OnMoveRequested()
    {
        if (!_hasSubmarine || !_isMyTurn || _movePending)
        {
            return;
        }

        _isMoveMode = !_isMoveMode;
        _moveOrigin = null;
        _yourBoard?.SetSelectedMoveCell(null);
        MoveHintText.IsVisible = _isMoveMode;
        MoveHintText.Text = _isMoveMode
            ? "Move mode: click a cell marked S to select your submarine."
            : "Click a cell marked S to select your submarine, then a straight-line destination up to 2 cells away. Moving uses your turn.";
        UpdateTurnText();
    }

    private async void OnYourCellClicked(int x, int y)
    {
        if (!_isMyTurn || !_isMoveMode || _movePending)
        {
            return;
        }

        var clicked = new GridPosition(x, y);
        if (_moveOrigin is null)
        {
            if (_yourBoard?.IsSubmarineCell(x, y) != true)
            {
                MoveHintText.Text = "That is not your submarine. Select a cell marked S on your board.";
                return;
            }

            _moveOrigin = clicked;
            _yourBoard?.SetSelectedMoveCell(clicked);
            MoveHintText.Text = "Submarine selected. Click a destination in a straight line, 1 or 2 cells away.";
            return;
        }

        var origin = _moveOrigin.Value;
        int deltaX = x - origin.X;
        int deltaY = y - origin.Y;
        int distance = Math.Abs(deltaX) + Math.Abs(deltaY);
        if ((deltaX == 0 && deltaY == 0) ||
            (deltaX != 0 && deltaY != 0) ||
            distance > 2)
        {
            MoveHintText.Text = "Invalid destination. Select a different cell in a straight line, up to 2 cells away.";
            return;
        }

        _movePending = true;
        UpdateTurnText();
        try
        {
            bool moved = await _connection.MoveShipAsync(origin.X, origin.Y, deltaX, deltaY);
            if (!moved)
            {
                _movePending = false;
                _moveOrigin = null;
                _yourBoard?.SetSelectedMoveCell(null);
                MoveHintText.Text = "Move rejected. Choose an undamaged submarine and a legal destination.";
                _controlsPanel.SetMoveButton(_hasSubmarine, _isMyTurn && !_movePending, _isMoveMode);
            }
        }
        catch (Exception ex)
        {
            _movePending = false;
            _moveOrigin = null;
            _yourBoard?.SetSelectedMoveCell(null);
            MoveHintText.Text = $"Could not move ship: {ex.Message}";
            _controlsPanel.SetMoveButton(_hasSubmarine, _isMyTurn && !_movePending, _isMoveMode);
        }
    }

    private void CancelMoveMode()
    {
        _isMoveMode = false;
        _moveOrigin = null;
        _movePending = false;
        MoveHintText.IsVisible = false;
        _yourBoard?.SetSelectedMoveCell(null);
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
        var turn = _isMyTurn ? "Your turn" : "Opponent's turn";
        TurnText.Text = _latestPowerUpNotice is null
            ? turn
            : $"{turn} — {_latestPowerUpNotice}";
        _controlsPanel.SetMoveButton(
            _hasSubmarine,
            _isMyTurn && !_movePending,
            _isMoveMode);
    }
}
