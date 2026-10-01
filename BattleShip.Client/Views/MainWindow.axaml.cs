using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using BattleShip.Shared;

namespace BattleShip.Client.Views;

public partial class MainWindow : Window
{
    private const string ServerUrl = "http://localhost:5058";

    private readonly ConnectionService _connection = new();
    private readonly ControlsPanel _controlsPanel;
    private readonly IShotStrategy _singleShotStrategy = new SingleShotStrategy();
    private readonly IShotStrategy _horizontalLineShotStrategy = new LineShotStrategy(ShotOrientation.Horizontal);
    private readonly IShotStrategy _verticalLineShotStrategy = new LineShotStrategy(ShotOrientation.Vertical);
    private readonly IShotStrategy _spreadShotStrategy = new SpreadShotStrategy();

    private BoardPanel? _yourBoard;
    private BoardPanel? _enemyBoard;
    private ScoreboardPanel? _scoreboard;

    private string? _myConnectionId;
    private bool _isMyTurn;

    public MainWindow()
    {
        InitializeComponent();

        _controlsPanel = new ControlsPanelFactory().Create();
        ControlsGrid.Children.Add(_controlsPanel);

        _connection.MatchFound += OnMatchFound;
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

    private void OnMatchFound(MatchFoundMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isMyTurn = msg.YouGoFirst;
            BuildGrids(msg.Board);
            BuildScoreboard();
            MenuPanel.IsVisible = false;
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
            GamePanel.IsVisible = false;
            MenuPanel.IsVisible = true;
            FindMatchButton.IsEnabled = true;
        });
    }

    private void BuildGrids(CellState[] board)
    {
        YourGrid.Children.Clear();
        EnemyGrid.Children.Clear();

        _yourBoard = new BoardPanelFactory(BoardCellOwner.Player, board).Create();
        YourGrid.Children.Add(_yourBoard);

        _enemyBoard = new BoardPanelFactory(BoardCellOwner.Enemy, board, OnEnemyCellClicked).Create();
        EnemyGrid.Children.Add(_enemyBoard);
    }

    private void BuildScoreboard()
    {
        _scoreboard = new ScoreboardPanelFactory().Create();

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