namespace BattleShip.Server;

/// <summary>
/// Serializable form of a <see cref="GameSession"/> that is written to and read from the saves folder.
/// </summary>
public class GameSessionSnapshot
{
    public string SessionId { get; set; } = string.Empty;
    public string LevelId { get; set; } = "Level1";
    public int[] GridA { get; set; } = [];
    public int[] GridB { get; set; } = [];
    public int[] ShotsA { get; set; } = [];
    public int[] ShotsB { get; set; } = [];
    public int[] PowerUpsA { get; set; } = [];
    public int[] PowerUpsB { get; set; } = [];
    public int ShotAreaUpA { get; set; }
    public int ShotAreaUpB { get; set; }
    public int ExtraTurnsA { get; set; }
    public int ExtraTurnsB { get; set; }
    public int FreeShotsA { get; set; }
    public int FreeShotsB { get; set; }
    public bool IsPlayerATurn { get; set; } = true;
    public bool PlayerAReady { get; set; }
    public bool PlayerBReady { get; set; }
    public bool IsGameStarted { get; set; }
    public DateTime Timestamp { get; set; }
}
