namespace BattleShip.Client;

internal interface IPlacementCommand
{
    bool Execute(out string? failureMessage);
    void Undo();
}
