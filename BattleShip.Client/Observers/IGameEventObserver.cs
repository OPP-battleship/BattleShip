using BattleShip.Shared;

namespace BattleShip.Client.Observers;

/// <summary>
/// Receives game events published by <see cref="ConnectionService"/>.
/// </summary>
public interface IGameEventObserver
{
    void OnPlacementStarted(PlacementStartedMessage message);

    void OnGameStarted(GameStartedMessage message);

    void OnGameResumed(GameResumedMessage message);

    void OnShotResult(ShotResultMessage message);

    void OnOpponentDisconnected(OpponentDisconnectedMessage message);
}
