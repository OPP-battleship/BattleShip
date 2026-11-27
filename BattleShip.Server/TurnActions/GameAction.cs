namespace BattleShip.Server.TurnActions;

/// <summary>
/// Root of every action a player can perform inside a <see cref="GameSession"/>.
/// <see cref="Execute"/> is declared overridable here so that <see cref="TurnAction"/>
/// can implement it as a <c>sealed override</c> template method (the C# equivalent of Java's <c>final</c>).
/// </summary>
internal abstract class GameAction
{
    public abstract bool Execute(GameSession session, string actorConnectionId);
}
