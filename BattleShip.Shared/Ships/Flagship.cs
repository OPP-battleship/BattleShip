namespace BattleShip.Shared;

public sealed class Flagship : Ship
{
    public override ShipType Type => ShipType.Flagship;

    internal Flagship()
    {
    }
}
