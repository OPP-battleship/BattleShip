namespace BattleShip.Shared;

public sealed class Submarine : Ship
{
    public override ShipType Type => ShipType.Submarine;

    internal Submarine()
    {
    }
}
