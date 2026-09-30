using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace BattleShip.Client;

public enum BoardCellOwner
{
    Player,
    Enemy,
}

public sealed class BoardFactory
{
    public Button CreateBoardCell(BoardCellOwner owner, bool hasShip, bool isObstacle)
    {
        var button = new Button
        {
            Content = owner == BoardCellOwner.Player && hasShip ? "X" : owner == BoardCellOwner.Player && isObstacle ? "#" : null,
            Margin = new Avalonia.Thickness(1),
        };

        switch (owner)
        {
            case BoardCellOwner.Player:
                button.Background = hasShip ? Brushes.SteelBlue : Brushes.Gray;
                button.IsHitTestVisible = false;
                break;
            case BoardCellOwner.Enemy:
                button.Background = Brushes.Gray;
                button.IsEnabled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(owner), owner, null);
        }

        if (owner == BoardCellOwner.Player && isObstacle)
        {
            button.Background = Brushes.Gray;
        }

        return button;
    }
}