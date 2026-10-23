using System.Text.Json;

namespace BattleShip.Server;

/// <summary>
/// Reads and writes game session snapshots as JSON in the "saves" folder of the current working directory.
/// </summary>
public static class GameSnapshotStore
{
    private const string FolderName = "saves";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string SaveDirectory => Path.Combine(Directory.GetCurrentDirectory(), FolderName);

    public static async Task<string> SaveAsync(GameSessionSnapshot snapshot)
    {
        Directory.CreateDirectory(SaveDirectory);

        var fileName = $"game_save_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
        var filePath = Path.Combine(SaveDirectory, fileName);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
        return fileName;
    }

    public static async Task<GameSessionSnapshot?> LoadLatestAsync()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            return null;
        }

        var latest = Directory.GetFiles(SaveDirectory, "game_save_*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (latest is null)
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(latest);
        return JsonSerializer.Deserialize<GameSessionSnapshot>(json, JsonOptions);
    }
}
