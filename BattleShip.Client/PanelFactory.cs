using Avalonia.Controls;

namespace BattleShip.Client;

public abstract class PanelFactory<TPanel> where TPanel : Control
{
    public TPanel Create() => CreatePanel();

    protected abstract TPanel CreatePanel();
}
