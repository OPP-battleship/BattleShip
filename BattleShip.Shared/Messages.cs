namespace BattleShip.Shared;

public record FireShotRequest(ShotTarget[] Targets);

public record MatchFoundMessage(string SessionId, bool YouGoFirst, CellState[] Board);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsHit, bool IsObstacle, bool IsYourTurnNext);

public record OpponentDisconnectedMessage(string SessionId);