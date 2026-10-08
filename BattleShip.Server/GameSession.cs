using BattleShip.Shared;

namespace BattleShip.Server;

public class GameSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
    public string PlayerAConnectionId { get; }
    public string PlayerBConnectionId { get; }

    public GridModel GridA { get; }
    public GridModel GridB { get; }

    public string CurrentTurnConnectionId { get; private set; }

    private readonly object _lock = new();
    private bool _playerAReady;
    private bool _playerBReady;
    private bool _isGameStarted;

    public GameSession(string playerA, string playerB, LevelFactory levelFactory)
    {
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
        GridA = new GridModel(
            levelFactory.GetObstacles(BoardLayout.PlayerA),
            levelFactory.GetPowerUps(BoardLayout.PlayerA));
        GridB = new GridModel(
            levelFactory.GetObstacles(BoardLayout.PlayerB),
            levelFactory.GetPowerUps(BoardLayout.PlayerB));
        CurrentTurnConnectionId = playerA; // player A always go first
    }

    public string OpponentOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? PlayerBConnectionId : PlayerAConnectionId;

    private GridModel GridOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? GridA : GridB;

    internal bool TryFireShotPattern(string shooterConnectionId, IReadOnlyList<ShotTarget> targets, out IReadOnlyList<ShotOutcome> outcomes)
    {
        outcomes = [];
        var results = new List<ShotOutcome>();

        lock (_lock)
        {
            if (!_isGameStarted) return false;
            if (shooterConnectionId != CurrentTurnConnectionId) return false;
            if (targets.Count == 0) return false;

            var defenderId = OpponentOf(shooterConnectionId);
            var defenderGrid = GridOf(defenderId);

            var seenTargets = new HashSet<ShotTarget>();
            foreach (var target in targets)
            {
                if (!seenTargets.Add(target))
                {
                    continue;
                }

                if (!GridModel.IsInsideBounds(target.X, target.Y))
                {
                    continue;
                }

                var cellState = defenderGrid.GetCell(target.X, target.Y);
                bool isHit = cellState == CellState.Ship;
                bool isObstacle = cellState == CellState.Obstacle;
                if (!defenderGrid.TryMarkFired(target.X, target.Y))
                {
                    continue;
                }

                results.Add(new ShotOutcome(target.X, target.Y, isHit, isObstacle));
            }

            if (results.Count == 0) return false;

            CurrentTurnConnectionId = defenderId;
            outcomes = results;
            return true;
        }
    }

    internal bool TrySubmitShipPlacement(string connectionId, IReadOnlyList<ShipPlacement>? placements, out bool gameStarted)
    {
        gameStarted = false;

        lock (_lock)
        {
            if (_isGameStarted || (connectionId != PlayerAConnectionId && connectionId != PlayerBConnectionId))
            {
                return false;
            }

            bool isPlayerA = connectionId == PlayerAConnectionId;
            if ((isPlayerA && _playerAReady) || (!isPlayerA && _playerBReady))
            {
                return false;
            }

            if (!ShipPlacementRules.HasCompleteFleet(placements) || !GridOf(connectionId).TryPlaceShips(placements))
            {
                return false;
            }

            if (isPlayerA)
            {
                _playerAReady = true;
            }
            else
            {
                _playerBReady = true;
            }

            if (_playerAReady && _playerBReady)
            {
                _isGameStarted = true;
                CurrentTurnConnectionId = PlayerAConnectionId;
                gameStarted = true;
            }

            return true;
        }
    }

    internal GameStartedMessage? CreateGameStartedMessage(string connectionId)
    {
        lock (_lock)
        {
            if (!_isGameStarted || (connectionId != PlayerAConnectionId && connectionId != PlayerBConnectionId))
            {
                return null;
            }

            return new GameStartedMessage(
                SessionId,
                YouGoFirst: connectionId == PlayerAConnectionId,
                GridOf(connectionId).ToFlatArray());
        }
    }
}
