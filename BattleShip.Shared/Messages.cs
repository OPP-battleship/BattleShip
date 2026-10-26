namespace BattleShip.Shared;

public record FireShotRequest(ShotStrategyType Strategy, int OriginX, int OriginY, ShotOrientation LineOrientation);

public record PlacementStartedMessage(string SessionId, CellState[] Board);

public record SubmitShipPlacementRequest(string SessionId, ShipPlacement[] Ships);

public record GameStartedMessage(string SessionId, bool YouGoFirst, CellState[] YourBoard, ShotStrategyType[] AvailableShotStrategies);

public record ShotMark(int X, int Y, bool IsHit, bool IsObstacle);

public record GameResumedMessage(string SessionId, bool IsYourTurn, CellState[] YourBoard, ShotMark[] ShotsAtYou, ShotMark[] ShotsByYou);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsHit, bool IsObstacle, bool IsYourTurnNext,
    ShotStrategyType[] AvailableShotStrategies,
    ShotStrategyType[] AvailableShotStrategies);

public record OpponentDisconnectedMessage(string SessionId);