using System.Collections.Concurrent;

namespace BattleShip.Server;

/// <summary>
/// Prototype manager: keeps named prototype sessions and hands out clones of them.
/// </summary>
public sealed class SessionPrototypeRegistry
{
    private readonly ConcurrentDictionary<string, GameSession> _prototypes = new();

    public void Register(string key, GameSession prototype) =>
        _prototypes[key] = prototype.Clone();

    public bool TrySpawn(string key, out GameSession? session)
    {
        if (_prototypes.TryGetValue(key, out var prototype))
        {
            session = prototype.Clone();
            return true;
        }

        session = null;
        return false;
    }
}
