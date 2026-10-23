namespace BattleShip.Shared;

/// <summary>
/// Prototype: declares an operation for creating a deep copy of an object
/// without coupling callers to its concrete construction logic.
/// </summary>
public interface IPrototype<out T>
{
    T Clone();
}
