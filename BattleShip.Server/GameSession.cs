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

    public GameSession(string playerA, string playerB, ILevelFactory levelFactory)
    {
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
        GridA = levelFactory.CreateGrid(BoardLayout.PlayerA);
        GridB = levelFactory.CreateGrid(BoardLayout.PlayerB);
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
}
