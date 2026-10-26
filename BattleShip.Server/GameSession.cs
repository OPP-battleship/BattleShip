using BattleShip.Shared;

namespace BattleShip.Server;

public class GameSession : IPrototype<GameSession>
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

    private GameSession(GameSession source)
    {
        lock (source._lock)
        {
            SessionId = source.SessionId;
            PlayerAConnectionId = source.PlayerAConnectionId;
            PlayerBConnectionId = source.PlayerBConnectionId;
            GridA = source.GridA.Clone();
            GridB = source.GridB.Clone();
            CurrentTurnConnectionId = source.CurrentTurnConnectionId;
            _playerAReady = source._playerAReady;
            _playerBReady = source._playerBReady;
            _isGameStarted = source._isGameStarted;
        }
    }

    private GameSession(GameSessionSnapshot snapshot, string playerA, string playerB)
    {
        SessionId = snapshot.SessionId;
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
        GridA = GridModel.FromFlatArray(snapshot.GridA.Select(c => (CellState)c).ToArray(), snapshot.ShotsA.Length == 0 ? null : snapshot.ShotsA);
        GridB = GridModel.FromFlatArray(snapshot.GridB.Select(c => (CellState)c).ToArray(), snapshot.ShotsB.Length == 0 ? null : snapshot.ShotsB);
        CurrentTurnConnectionId = snapshot.IsPlayerATurn ? playerA : playerB;
        _playerAReady = snapshot.PlayerAReady;
        _playerBReady = snapshot.PlayerBReady;
        _isGameStarted = snapshot.IsGameStarted;
    }

    public GameSession Clone() => new(this);

    public bool IsGameStarted
    {
        get
        {
            lock (_lock)
            {
                return _isGameStarted;
            }
        }
    }

    public GameSessionSnapshot ToSnapshot()
    {
        lock (_lock)
        {
            return new GameSessionSnapshot
            {
                SessionId = SessionId,
                GridA = GridA.ToFlatArray().Select(c => (int)c).ToArray(),
                GridB = GridB.ToFlatArray().Select(c => (int)c).ToArray(),
                ShotsA = GridA.ToShotCodes(),
                ShotsB = GridB.ToShotCodes(),
                IsPlayerATurn = CurrentTurnConnectionId == PlayerAConnectionId,
                PlayerAReady = _playerAReady,
                PlayerBReady = _playerBReady,
                IsGameStarted = _isGameStarted,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public static GameSession FromSnapshot(GameSessionSnapshot snapshot, string playerA, string playerB) =>
        new(snapshot, playerA, playerB);

    public string OpponentOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? PlayerBConnectionId : PlayerAConnectionId;

    private GridModel GridOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? GridA : GridB;

    internal bool TryFireShotPattern(string shooterConnectionId, FireShotRequest request, out IReadOnlyList<ShotOutcome> outcomes)
    {
        outcomes = [];
        var results = new List<ShotOutcome>();

        lock (_lock)
        {
            if (!_isGameStarted) return false;
            if (shooterConnectionId != CurrentTurnConnectionId) return false;
            if (request is null ||
                !Enum.IsDefined(request.Strategy) ||
                !Enum.IsDefined(request.LineOrientation) ||
                !GridModel.IsInsideBounds(request.OriginX, request.OriginY))
            {
                return false;
            }

            var shooterGrid = GridOf(shooterConnectionId);
            if (!shooterGrid.GetAvailableShotStrategies().Contains(request.Strategy))
            {
                return false;
            }

            var targets = ShotStrategyFactory.Create(request.Strategy, request.LineOrientation)
                .GetTargets(request.OriginX, request.OriginY);
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

    internal GameResumedMessage? CreateGameResumedMessage(string connectionId)
    {
        lock (_lock)
        {
            if (!_isGameStarted || (connectionId != PlayerAConnectionId && connectionId != PlayerBConnectionId))
            {
                return null;
            }

            var ownGrid = GridOf(connectionId);
            var enemyGrid = GridOf(OpponentOf(connectionId));

            return new GameResumedMessage(
                SessionId,
                IsYourTurn: connectionId == CurrentTurnConnectionId,
                ownGrid.ToUnfiredFlatArray(),
                ownGrid.GetShotMarks().ToArray(),
                enemyGrid.GetShotMarks().ToArray());
        }
    }

    internal PlacementStartedMessage CreatePlacementStartedMessage(string connectionId)
    {
        lock (_lock)
        {
            return new PlacementStartedMessage(SessionId, GridOf(connectionId).ToFlatArray());
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
                GridOf(connectionId).ToFlatArray(),
                GridOf(connectionId).GetAvailableShotStrategies().ToArray());
        }
    }

    internal IReadOnlyList<ShotStrategyType> GetAvailableShotStrategies(string connectionId)
    {
        lock (_lock)
        {
            return GridOf(connectionId).GetAvailableShotStrategies();
        }
    }
}
