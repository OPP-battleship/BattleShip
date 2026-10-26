using BattleShip.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.Server;

public class GameHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        SessionManager.Instance.RegisterConnection(Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public async Task FindMatch()
    {
        if (IsDebugConnection)
        {
            await StartDebugSessionAsync();
            return;
        }

        var session = SessionManager.Instance.TryMatch(Context.ConnectionId);
        if (session is null)
        {
            return; // waiting in queue
        }

        var opponentId = session.OpponentOf(Context.ConnectionId);

        await Groups.AddToGroupAsync(opponentId, session.SessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.SessionId);

        await Clients.Client(opponentId)
            .SendAsync("PlacementStarted", new PlacementStartedMessage(
                session.SessionId,
                session.GridA.ToFlatArray()));

        await Clients.Client(Context.ConnectionId)
            .SendAsync("PlacementStarted", new PlacementStartedMessage(
                session.SessionId,
                session.GridB.ToFlatArray()));
    }

    private bool IsDebugConnection =>
        string.Equals(Context.GetHttpContext()?.Request.Query["debug"], "true", StringComparison.OrdinalIgnoreCase);

    private async Task StartDebugSessionAsync()
    {
        var snapshot = await GameSnapshotStore.LoadLatestAsync();
        if (snapshot is null)
        {
            throw new HubException("No saved game found in the saves folder.");
        }

        var session = SessionManager.Instance.TryStartDebugSession(Context.ConnectionId, snapshot);
        if (session is null)
        {
            return; // waiting for the second debug client
        }

        foreach (var playerId in new[] { session.PlayerAConnectionId, session.PlayerBConnectionId })
        {
            await Groups.AddToGroupAsync(playerId, session.SessionId);

            var resumed = session.CreateGameResumedMessage(playerId);
            if (resumed is not null)
            {
                await Clients.Client(playerId).SendAsync("GameResumed", resumed);
            }
            else
            {
                await Clients.Client(playerId).SendAsync("PlacementStarted", session.CreatePlacementStartedMessage(playerId));
            }
        }
    }

    public async Task<string> SaveGame()
    {
        if (!SessionManager.Instance.TryGetSession(Context.ConnectionId, out var session) || session is null)
        {
            throw new HubException("No active game to save.");
        }

        var prototype = session.Clone();
        SessionManager.Instance.Prototypes.Register("saved", prototype);
        return await GameSnapshotStore.SaveAsync(prototype.ToSnapshot());
    }

    public async Task SubmitShipPlacement(SubmitShipPlacementRequest request)
    {
        if (request is null ||
            !SessionManager.Instance.TryGetSession(Context.ConnectionId, out var session) ||
            session is null ||
            request.SessionId != session.SessionId)
        {
            return;
        }

        bool started;
        var placements = request.Ships ?? Array.Empty<ShipPlacement>();
        if (!session.TrySubmitShipPlacement(Context.ConnectionId, placements, out started) || !started)
        {
            return;
        }

        await Clients.Client(session.PlayerAConnectionId)
            .SendAsync("GameStarted", session.CreateGameStartedMessage(session.PlayerAConnectionId));
        await Clients.Client(session.PlayerBConnectionId)
            .SendAsync("GameStarted", session.CreateGameStartedMessage(session.PlayerBConnectionId));
    }

    public async Task FireShot(FireShotRequest request)
    {
        if (request is null) return;
        if (!SessionManager.Instance.TryGetSession(Context.ConnectionId, out var session) || session is null) return;

        bool success = session.TryFireShotPattern(Context.ConnectionId, request, out var outcomes);
        if (!success) return; // Invalid move

        var opponentId = session.OpponentOf(Context.ConnectionId);
        var lastIndex = outcomes.Count - 1;
        var shooterStrategies = session.GetAvailableShotStrategies(Context.ConnectionId).ToArray();
        var defenderStrategies = session.GetAvailableShotStrategies(opponentId).ToArray();

        for (int i = 0; i < outcomes.Count; i++)
        {
            var outcome = outcomes[i];
            bool isYourTurnNext = i == lastIndex;

            await Clients.Client(Context.ConnectionId)
                .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, outcome.X, outcome.Y, outcome.IsHit, outcome.IsObstacle,
                    IsYourTurnNext: false, AvailableShotStrategies: shooterStrategies));

            await Clients.Client(opponentId)
                .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, outcome.X, outcome.Y, outcome.IsHit, outcome.IsObstacle,
                    IsYourTurnNext: isYourTurnNext, AvailableShotStrategies: defenderStrategies));
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (SessionManager.Instance.TryRemoveSession(Context.ConnectionId, out var session) && session is not null)
        {
            var opponentId = session.OpponentOf(Context.ConnectionId);
            await Clients.Client(opponentId)
                .SendAsync("OpponentDisconnected", new OpponentDisconnectedMessage(session.SessionId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
