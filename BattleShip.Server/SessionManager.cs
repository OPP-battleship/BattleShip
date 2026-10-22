using System.Collections.Concurrent;
using BattleShip.Shared;

namespace BattleShip.Server;

public sealed class SessionManager
{
    private static readonly SessionManager _instance = new SessionManager();
    private const string DebugPrototypeKey = "debug";

    private readonly ConcurrentQueue<string> WaitingQueue = new();
    private readonly ConcurrentDictionary<string, GameSession> SessionsByConnection = new();
    private readonly HashSet<string> WaitingConnectionIds = [];
    private readonly HashSet<string> ConnectedConnectionIds = [];
    private readonly object _matchmakingLock = new();
    private string? _pendingDebugConnectionId;

    public SessionPrototypeRegistry Prototypes { get; } = new();

    private SessionManager()
    {
    }

    public static SessionManager Instance => _instance;

    public void RegisterConnection(string connectionId)
    {
        lock (_matchmakingLock)
        {
            ConnectedConnectionIds.Add(connectionId);
        }
    }

    public GameSession? TryMatch(string connectionId)
    {
        string? opponentId = null;

        lock (_matchmakingLock)
        {
            if (SessionsByConnection.ContainsKey(connectionId))
            {
                return null;
            }

            while (WaitingQueue.TryDequeue(out var waiting))
            {
                if (!WaitingConnectionIds.Remove(waiting) ||
                    !ConnectedConnectionIds.Contains(waiting) ||
                    waiting == connectionId ||
                    SessionsByConnection.ContainsKey(waiting))
                {
                    continue;
                }

                opponentId = waiting;
                break;
            }

            if (opponentId is null)
            {
                if (WaitingConnectionIds.Add(connectionId))
                {
                    WaitingQueue.Enqueue(connectionId);
                }

                return null;
            }

            LevelFactory levelFactory = Random.Shared.Next(1, 3) == 1
                ? new Level1Factory()
                : new Level2Factory();
            var session = new GameSession(opponentId, connectionId, levelFactory);
            SessionsByConnection[opponentId] = session;
            SessionsByConnection[connectionId] = session;
            return session;
        }
    }

    public GameSession? TryStartDebugSession(string connectionId, GameSessionSnapshot snapshot)
    {
        lock (_matchmakingLock)
        {
            if (SessionsByConnection.ContainsKey(connectionId))
            {
                return null;
            }

            if (_pendingDebugConnectionId is null ||
                _pendingDebugConnectionId == connectionId ||
                !ConnectedConnectionIds.Contains(_pendingDebugConnectionId))
            {
                _pendingDebugConnectionId = connectionId;
                return null;
            }

            var playerA = _pendingDebugConnectionId;
            _pendingDebugConnectionId = null;

            Prototypes.Register(DebugPrototypeKey, GameSession.FromSnapshot(snapshot, playerA, connectionId));
            if (!Prototypes.TrySpawn(DebugPrototypeKey, out var session) || session is null)
            {
                return null;
            }

            SessionsByConnection[playerA] = session;
            SessionsByConnection[connectionId] = session;
            return session;
        }
    }

    public bool TryGetSession(string connectionId, out GameSession? session) =>
        SessionsByConnection.TryGetValue(connectionId, out session);

    public bool TryRemoveSession(string connectionId, out GameSession? session)
    {
        lock (_matchmakingLock)
        {
            ConnectedConnectionIds.Remove(connectionId);
            WaitingConnectionIds.Remove(connectionId);

            if (_pendingDebugConnectionId == connectionId)
            {
                _pendingDebugConnectionId = null;
            }

            if (!SessionsByConnection.TryRemove(connectionId, out session))            {
                return false;
            }

            SessionsByConnection.TryRemove(session.OpponentOf(connectionId), out _);
            return true;
        }
    }
}
