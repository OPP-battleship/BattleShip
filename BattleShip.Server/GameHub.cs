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
        var session = SessionManager.Instance.TryMatch(Context.ConnectionId);
        if (session is null)
        {
            return; // waiting in queue
        }

        var opponentId = session.OpponentOf(Context.ConnectionId);

        await Groups.AddToGroupAsync(opponentId, session.SessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.SessionId);

        await Clients.Client(opponentId)
            .SendAsync("MatchFound", new MatchFoundMessage(session.SessionId, YouGoFirst: true));

        await Clients.Client(Context.ConnectionId)
            .SendAsync("MatchFound", new MatchFoundMessage(session.SessionId, YouGoFirst: false));
    }

    public async Task FireShot(FireShotRequest request)
    {
        if (!SessionManager.Instance.TryGetSession(Context.ConnectionId, out var session) || session is null) return;

        var targets = request.Targets ?? Array.Empty<ShotTarget>();
        bool success = session.TryFireShotPattern(Context.ConnectionId, targets, out var outcomes);
        if (!success) return; // Invalid move

        var opponentId = session.OpponentOf(Context.ConnectionId);
        var lastIndex = outcomes.Count - 1;

        for (int i = 0; i < outcomes.Count; i++)
        {
            var outcome = outcomes[i];
            bool isYourTurnNext = i == lastIndex;

            await Clients.Client(Context.ConnectionId)
                .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, outcome.X, outcome.Y, outcome.IsHit, IsYourTurnNext: false));

            await Clients.Client(opponentId)
                .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, outcome.X, outcome.Y, outcome.IsHit, IsYourTurnNext: isYourTurnNext));
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
