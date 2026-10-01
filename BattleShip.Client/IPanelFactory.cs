using Avalonia.Controls;

namespace BattleShip.Client;

public interface IPanelFactory<out TPanel> where TPanel : Control
{
    TPanel Create();
}
