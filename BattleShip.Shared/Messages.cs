namespace BattleShip.Shared;

public record FireShotRequest(ShotStrategyType Strategy, int OriginX, int OriginY, ShotOrientation LineOrientation);

public record MoveShipRequest(int FromX, int FromY, int DeltaX, int DeltaY);

public record PlacementStartedMessage(string SessionId, CellState[] Board);

public record PlacementCompletedMessage(string SessionId, CellState[] Board);

public record SubmitShipPlacementRequest(string SessionId, ShipPlacement[] Ships);

public record GameStartedMessage(
    string SessionId,
    bool YouGoFirst,
    CellState[] YourBoard,
    ShotStrategyType[] AvailableShotStrategies,
    bool HasSubmarine,
    GridPosition[]? SubmarineCells = null);

public record ShotMark(int X, int Y, bool IsHit, bool IsObstacle);

public record GameResumedMessage(
    string SessionId,
    bool IsYourTurn,
    CellState[] YourBoard,
    ShotMark[] ShotsAtYou,
    ShotMark[] ShotsByYou,
    bool HasSubmarine,
    GridPosition[]? SubmarineCells = null);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsHit, bool IsObstacle, bool IsYourTurnNext,
    ShotStrategyType[] AvailableShotStrategies, PowerUpType? CollectedPowerUp = null);

public record ShipMovedMessage(
    string MovingPlayerConnectionId,
    bool IsYourTurnNext,
    CellState[]? YourBoard = null,
    ShotMark[]? YourShotsAtYou = null,
    GridPosition[]? SubmarineCells = null);

public record OpponentDisconnectedMessage(string SessionId);