using System;
using System.Diagnostics;
using BattleShip.Shared;

namespace BattleShip.Client.Observers;

/// <summary>
/// Adds an audit entry before forwarding each notification.
/// </summary>
public sealed class LoggingGameEventObserverDecorator : GameEventObserverDecorator
{
    private readonly Action<string> _writeLog;

    public LoggingGameEventObserverDecorator(
        IGameEventObserver inner,
        Action<string>? writeLog = null)
        : base(inner)
    {
        _writeLog = writeLog ?? (entry => Debug.WriteLine(entry));
    }

    public override void OnPlacementStarted(PlacementStartedMessage message)
    {
        Write($"Placement started for session {message.SessionId}.");
        base.OnPlacementStarted(message);
    }

    public override void OnGameStarted(GameStartedMessage message)
    {
        Write($"Game started for session {message.SessionId}; you go first: {message.YouGoFirst}.");
        base.OnGameStarted(message);
    }

    public override void OnShotResult(ShotResultMessage message)
    {
        Write($"Shot result at ({message.X}, {message.Y}); hit: {message.IsHit}; obstacle: {message.IsObstacle}.");
        base.OnShotResult(message);
    }

    public override void OnOpponentDisconnected(OpponentDisconnectedMessage message)
    {
        Write($"Opponent disconnected from session {message.SessionId}.");
        base.OnOpponentDisconnected(message);
    }

    private void Write(string message) =>
        _writeLog($"[{DateTimeOffset.Now:O}] {message}");
}
