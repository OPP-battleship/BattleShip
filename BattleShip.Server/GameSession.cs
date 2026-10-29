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

    private readonly LevelFactory _levelFactory;
    private readonly object _lock = new();
    private bool _playerAReady;
    private bool _playerBReady;
    private bool _isGameStarted;

    public GameSession(string playerA, string playerB, LevelFactory levelFactory)
    {
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
        _levelFactory = levelFactory;
        GridA = new GridModel(
            levelFactory.GetObstacles(BoardLayout.PlayerA),
            Array.Empty<PowerUp>());
        GridB = new GridModel(
            levelFactory.GetObstacles(BoardLayout.PlayerB),
            Array.Empty<PowerUp>());
        CurrentTurnConnectionId = playerA; // player A always go first
    }

    private GameSession(GameSession source)
    {
        lock (source._lock)
        {
            SessionId = source.SessionId;
            PlayerAConnectionId = source.PlayerAConnectionId;
            PlayerBConnectionId = source.PlayerBConnectionId;
            _levelFactory = source._levelFactory;
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
        _levelFactory = snapshot.LevelId switch
        {
            "Level1" => new Level1Factory(),
            "Level2" => new Level2Factory(),
            _ => throw new ArgumentException($"Unknown level '{snapshot.LevelId}'.", nameof(snapshot))
        };
        GridA = GridModel.FromFlatArray(
            snapshot.GridA.Select(c => (CellState)c).ToArray(),
            snapshot.ShotsA.Length == 0 ? null : snapshot.ShotsA,
            snapshot.PowerUpsA.Length == 0 ? null : snapshot.PowerUpsA,
            snapshot.ShotAreaUpA,
            snapshot.ExtraTurnsA,
            snapshot.FreeShotsA,
            snapshot.ShipsA);
        GridB = GridModel.FromFlatArray(
            snapshot.GridB.Select(c => (CellState)c).ToArray(),
            snapshot.ShotsB.Length == 0 ? null : snapshot.ShotsB,
            snapshot.PowerUpsB.Length == 0 ? null : snapshot.PowerUpsB,
            snapshot.ShotAreaUpB,
            snapshot.ExtraTurnsB,
            snapshot.FreeShotsB,
            snapshot.ShipsB);
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
                LevelId = _levelFactory.LevelId,
                GridA = GridA.ToFlatArray().Select(c => (int)c).ToArray(),
                GridB = GridB.ToFlatArray().Select(c => (int)c).ToArray(),
                ShipsA = GridA.GetShipSnapshots(),
                ShipsB = GridB.GetShipSnapshots(),
                ShotsA = GridA.ToShotCodes(),
                ShotsB = GridB.ToShotCodes(),
                PowerUpsA = GridA.ToPowerUpCodes(),
                PowerUpsB = GridB.ToPowerUpCodes(),
                ShotAreaUpA = GridA.GetPowerUpCounts().ShotAreaUp,
                ShotAreaUpB = GridB.GetPowerUpCounts().ShotAreaUp,
                ExtraTurnsA = GridA.GetPowerUpCounts().ExtraTurn,
                ExtraTurnsB = GridB.GetPowerUpCounts().ExtraTurn,
                FreeShotsA = GridA.GetPowerUpCounts().FreeShot,
                FreeShotsB = GridB.GetPowerUpCounts().FreeShot,
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

            bool shotAreaUpActive = shooterGrid.HasShotAreaUp;
            bool freeShotActive = shooterGrid.HasFreeShot;
            IShotStrategy strategy = ShotStrategyFactory.Create(request.Strategy, request.LineOrientation);
            if (shotAreaUpActive)
            {
                strategy = new ShotAreaUpShotStrategyAdapter(strategy);
            }

            var targets = strategy
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
                if (!defenderGrid.TryMarkFired(target.X, target.Y, out var powerUp))
                {
                    continue;
                }

                PowerUpType? collectedPowerUp = null;
                if (powerUp is not null)
                {
                    new PowerUpAdapter(powerUp).ApplyTo(shooterGrid);
                    collectedPowerUp = PowerUpAdapter.GetPowerUpType(powerUp);
                }

                results.Add(new ShotOutcome(target.X, target.Y, isHit, isObstacle, collectedPowerUp));
            }

            if (results.Count == 0) return false;

            if (shotAreaUpActive)
            {
                shooterGrid.TryConsumeShotAreaUp();
            }

            if (freeShotActive)
            {
                shooterGrid.TryConsumeFreeShot();
            }

            CurrentTurnConnectionId = shooterGrid.TryConsumeExtraTurn()
                ? shooterConnectionId
                : defenderId;
            outcomes = results;
            return true;
        }
    }

    internal bool TryMoveShip(string movingConnectionId, MoveShipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            if (!_isGameStarted ||
                movingConnectionId != CurrentTurnConnectionId ||
                (movingConnectionId != PlayerAConnectionId && movingConnectionId != PlayerBConnectionId) ||
                !GridOf(movingConnectionId).TryMoveShip(
                    request.FromX, request.FromY, request.DeltaX, request.DeltaY))
            {
                return false;
            }

            CurrentTurnConnectionId = OpponentOf(movingConnectionId);
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

            var boardLayout = isPlayerA ? BoardLayout.PlayerA : BoardLayout.PlayerB;
            var grid = GridOf(connectionId);
            var powerUps = _levelFactory.GetPowerUps(boardLayout, grid);
            if (!grid.TryAddPowerUps(powerUps))
            {
                throw new InvalidOperationException("Generated power-ups conflict with the placed fleet.");
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

    internal PlacementCompletedMessage? CreatePlacementCompletedMessage(string connectionId)
    {
        lock (_lock)
        {
            if (connectionId != PlayerAConnectionId && connectionId != PlayerBConnectionId)
            {
                return null;
            }

            return new PlacementCompletedMessage(SessionId, GridOf(connectionId).ToFlatArray());
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

    internal ShipMovedMessage CreateShipMovedMessage(string receivingConnectionId, string movingConnectionId)
    {
        lock (_lock)
        {
            bool isMovingPlayer = receivingConnectionId == movingConnectionId;
            var ownGrid = GridOf(receivingConnectionId);
            return new ShipMovedMessage(
                movingConnectionId,
                IsYourTurnNext: receivingConnectionId == CurrentTurnConnectionId,
                YourBoard: isMovingPlayer ? ownGrid.ToUnfiredFlatArray() : null,
                YourShotsAtYou: isMovingPlayer ? ownGrid.GetShotMarks().ToArray() : null);
        }
    }

    internal IReadOnlyList<ShotStrategyType> GetAvailableShotStrategies(string connectionId)
    {
        lock (_lock)
        {
            return GridOf(connectionId).GetAvailableShotStrategies();
        }
    }

    internal bool IsPlayersTurn(string connectionId)
    {
        lock (_lock)
        {
            return CurrentTurnConnectionId == connectionId;
        }
    }
}
