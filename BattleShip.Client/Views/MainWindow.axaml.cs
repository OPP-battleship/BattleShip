using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using BattleShip.Client.Observers;
using BattleShip.Shared;

namespace BattleShip.Client.Views;

public partial class MainWindow : Window, IGameEventObserver, IPlacementCommandReceiver
{
    private const string ServerUrl = "http://localhost:5058";

    private readonly GameFacade _gameFacade = new(new ConnectionService());
    private readonly ShipPlacementClient _shipPlacementClient;
    private readonly IGameEventObserver _gameEventObserver;
    private readonly ControlsPanel _controlsPanel;
    private readonly ShipPanel _shipPanel;

    private BoardPanel? _yourBoard;
    private BoardPanel? _enemyBoard;
    private BoardPanel? _placementBoard;
    private ScoreboardPanel? _scoreboard;
    private readonly PlacementCommandInvoker _placementCommands = new();
    private CellState[]? _placementBoardState;

    private string? _myConnectionId;
    private string? _sessionId;
    private string? _latestPowerUpNotice;
    private bool _isMyTurn;

    public MainWindow()
    {
        InitializeComponent();
        _shipPlacementClient = new ShipPlacementClient(_gameFacade);

        _controlsPanel = (ControlsPanel)PanelFactory.Create(PanelFactory.Controls);
        ControlsGrid.Children.Add(_controlsPanel);

        _shipPanel = (ShipPanel)PanelFactory.Create(PanelFactory.Ships);
        _shipPanel.ReadyRequested += OnReadyRequested;
        _shipPanel.UndoRequested += OnUndoRequested;
        ShipSelectionGrid.Children.Add(_shipPanel);

        _gameEventObserver = new UiThreadGameEventObserverDecorator(
            new ExceptionHandlingGameEventObserverDecorator(
                new LoggingGameEventObserverDecorator(this)));
        _gameFacade.Subscribe(_gameEventObserver);
        Closed += (_, _) => _gameFacade.Unsubscribe(_gameEventObserver);
    }

    private async void OnFindMatchClicked(object? sender, RoutedEventArgs e)
    {
        FindMatchButton.IsEnabled = false;
        StatusText.Text = "Connecting...";

        try
        {
            await _gameFacade.ConnectAsync(ServerUrl);
            _myConnectionId = _gameFacade.MyConnectionId;

            StatusText.Text = "Searching for an opponent...";
            await _gameFacade.FindMatchAsync();
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
        _shipPlacementClient.BeginPlacement(msg.Board);
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
        _isMyTurn = msg.YouGoFirst;
        _controlsPanel.SetAvailableShotStrategies(msg.AvailableShotStrategies);
        BuildGrids(msg.YourBoard);
        BuildScoreboard();
        PlacementPanel.IsVisible = false;
        GamePanel.IsVisible = true;
        UpdateTurnText();
    }

    public void OnGameResumed(GameResumedMessage msg)
    {
        _sessionId = msg.SessionId;
        _myConnectionId = _gameFacade.MyConnectionId;
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
        _shipPlacementClient.Clear();
        _placementBoard = null;
        _placementBoardState = null;
        _placementCommands.Clear();
        _shipPanel.Reset();
        _shipPanel.SetUndoEnabled(false);
        _latestPowerUpNotice = null;
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
            await _gameFacade.SubmitShipPlacementAsync(_sessionId, _shipPlacementClient.Placements);
        }
        catch (Exception ex)
        {
            _shipPanel.CancelReady();
            PlacementStatusText.Text = $"Could not submit fleet: {ex.Message}";
        }
    }

    public bool CanPlaceShip(ShipPlacement placement, out string failureMessage)
    {
        return _shipPlacementClient.CanPlaceShip(placement, out failureMessage);
    }

    public void CommitPlacement(ShipPlacement placement)
    {
        _shipPlacementClient.CommitPlacement(placement);
        RefreshPlacementBoard();
        _shipPanel.MarkShipPlaced(placement.Type);
        PlacementStatusText.Text = _shipPanel.HasCompleteFleet
            ? "All points spent. Select Ready to start when your opponent is ready."
            : $"Placed {ShipCatalogue.GetDisplayName(placement.Type)}. {_shipPanel.PointsRemaining} points remaining.";
    }

    public void RevertPlacement(ShipPlacement placement)
    {
        if (!_shipPlacementClient.RevertPlacement(placement))
        {
            return;
        }

        RefreshPlacementBoard();
        _shipPanel.MarkShipRemoved(placement.Type);
        PlacementStatusText.Text = _shipPlacementClient.Placements.Count == 0
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
        foreach (var placement in _shipPlacementClient.Placements)
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
        foreach (var placement in _shipPlacementClient.Placements)
        {
            _yourBoard.ApplyShipPlacement(placement);
        }

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

        var lineOrientation = _controlsPanel.LineOrientationSelectedIndex == 1
            ? ShotOrientation.Vertical
            : ShotOrientation.Horizontal;
        await _gameFacade.FireShotAsync(_controlsPanel.SelectedShotStrategy, x, y, lineOrientation);
    }

    private async void OnSaveGameClicked(object? sender, RoutedEventArgs e)
    {
        SaveGameButton.IsEnabled = false;
        try
        {
            var fileName = await _gameFacade.SaveGameAsync();
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
    }
}
