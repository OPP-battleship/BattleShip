namespace BattleShip.Shared;

public sealed class Destroyer : Ship
{
    public override ShipType Type => ShipType.Destroyer;

    internal Destroyer()
    {
    }
}
