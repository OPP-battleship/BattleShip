using System;
using BattleShip.Shared;

namespace BattleShip.Client.Observers;

/// <summary>
/// Base decorator that forwards every notification to a wrapped observer.
/// </summary>
public abstract class GameEventObserverDecorator(IGameEventObserver inner) : IGameEventObserver
{
    protected IGameEventObserver Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    public virtual void OnPlacementStarted(PlacementStartedMessage message) =>
        Inner.OnPlacementStarted(message);

    public virtual void OnGameStarted(GameStartedMessage message) =>
        Inner.OnGameStarted(message);

    public virtual void OnShotResult(ShotResultMessage message) =>
        Inner.OnShotResult(message);

    public virtual void OnOpponentDisconnected(OpponentDisconnectedMessage message) =>
        Inner.OnOpponentDisconnected(message);
}
