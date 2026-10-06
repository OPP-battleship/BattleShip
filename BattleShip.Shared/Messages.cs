namespace BattleShip.Shared;

public record FireShotRequest(ShotTarget[] Targets);

public record PlacementStartedMessage(string SessionId, CellState[] Board);

public record SubmitShipPlacementRequest(string SessionId, ShipPlacement[] Ships);

public record GameStartedMessage(string SessionId, bool YouGoFirst, CellState[] YourBoard);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsHit, bool IsObstacle, bool IsYourTurnNext);

public record OpponentDisconnectedMessage(string SessionId);