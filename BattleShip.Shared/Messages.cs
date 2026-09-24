namespace BattleShip.Shared;

public record FireShotRequest(ShotTarget[] Targets);

public record MatchFoundMessage(string SessionId, bool YouGoFirst);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsHit, bool IsYourTurnNext);

public record OpponentDisconnectedMessage(string SessionId);