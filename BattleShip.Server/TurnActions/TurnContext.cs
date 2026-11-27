using BattleShip.Shared;

namespace BattleShip.Server.TurnActions;

/// <summary>Data the template method hands to every step of a turn.</summary>
internal readonly record struct TurnContext(
    string ActorConnectionId,
    string OpponentConnectionId,
    GridModel ActorGrid,
    GridModel OpponentGrid);
