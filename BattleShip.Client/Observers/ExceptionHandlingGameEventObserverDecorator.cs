using System;
using System.Diagnostics;
using BattleShip.Shared;

namespace BattleShip.Client.Observers;

/// <summary>
/// Keeps a failing consumer from propagating an exception into SignalR's receive callback.
/// </summary>
public sealed class ExceptionHandlingGameEventObserverDecorator : GameEventObserverDecorator
{
    private readonly Action<string, Exception> _handleException;

    public ExceptionHandlingGameEventObserverDecorator(
        IGameEventObserver inner,
        Action<string, Exception>? handleException = null)
        : base(inner)
    {
        _handleException = handleException ?? ((eventName, exception) =>
            Debug.WriteLine($"Game event '{eventName}' failed: {exception}"));
    }

    public override void OnPlacementStarted(PlacementStartedMessage message) =>
        Execute(nameof(OnPlacementStarted), () => base.OnPlacementStarted(message));

    public override void OnGameStarted(GameStartedMessage message) =>
        Execute(nameof(OnGameStarted), () => base.OnGameStarted(message));

    public override void OnShotResult(ShotResultMessage message) =>
        Execute(nameof(OnShotResult), () => base.OnShotResult(message));

    public override void OnOpponentDisconnected(OpponentDisconnectedMessage message) =>
        Execute(nameof(OnOpponentDisconnected), () => base.OnOpponentDisconnected(message));

    private void Execute(string eventName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _handleException(eventName, exception);
        }
    }
}
