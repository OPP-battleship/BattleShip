using BattleShip.Shared;

namespace BattleShip.Server;

public class GameSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
    public string PlayerAConnectionId { get; }
    public string PlayerBConnectionId { get; }

    public GridModel GridA { get; } = new();
    public GridModel GridB { get; } = new();

    public string CurrentTurnConnectionId { get; private set; }

    private readonly object _lock = new();

    public GameSession(string playerA, string playerB)
    {
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
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
            isHit = defenderGrid.GetCell(x, y) == CellState.Ship;

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

                bool isHit = defenderGrid.GetCell(target.X, target.Y) == CellState.Ship;
                if (!defenderGrid.TryMarkFired(target.X, target.Y))
                {
                    continue;
                }

                results.Add(new ShotOutcome(target.X, target.Y, isHit));
            }

            if (results.Count == 0) return false;

            CurrentTurnConnectionId = defenderId;
            outcomes = results;
            return true;
        }
    }
}