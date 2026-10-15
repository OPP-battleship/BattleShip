using System;
using Avalonia.Threading;
using BattleShip.Shared;

namespace BattleShip.Client.Observers;

/// <summary>
/// Marshals notifications from SignalR's receive thread to Avalonia's UI thread.
/// </summary>
public sealed class UiThreadGameEventObserverDecorator(IGameEventObserver inner)
    : GameEventObserverDecorator(inner)
{
    public override void OnPlacementStarted(PlacementStartedMessage message) =>
        Dispatch(() => base.OnPlacementStarted(message));

    public override void OnGameStarted(GameStartedMessage message) =>
        Dispatch(() => base.OnGameStarted(message));

    public override void OnShotResult(ShotResultMessage message) =>
        Dispatch(() => base.OnShotResult(message));

    public override void OnOpponentDisconnected(OpponentDisconnectedMessage message) =>
        Dispatch(() => base.OnOpponentDisconnected(message));

    private static void Dispatch(Action notification)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            notification();
            return;
        }

        Dispatcher.UIThread.Post(notification);
    }
}
