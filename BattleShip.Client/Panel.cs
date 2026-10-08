using Avalonia.Controls;

namespace BattleShip.Client;

public abstract class Panel : UserControl
{
    protected Panel(Avalonia.Controls.Panel layout)
    {
        Content = layout;
    }
}
