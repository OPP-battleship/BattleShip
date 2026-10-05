using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BattleShip.Shared;
using System;

namespace BattleShip.Client;

public sealed class BoardPanelFactory : PanelFactory<Panel>
{
    private readonly BoardCellOwner _owner;
    private readonly CellState[] _board;
    private readonly Action<int, int>? _cellClicked;

    public BoardPanelFactory(BoardCellOwner owner, CellState[] board,
        Action<int, int>? cellClicked = null)
    {
        _owner = owner;
        _board = board;
        _cellClicked = cellClicked;
    }

    protected override Panel CreatePanel()
    {
        var grid = new UniformGrid
        {
            Rows = GridModel.Size,
            Columns = GridModel.Size,
            Width = 400,
            Height = 400
        };

        var cells = new Button[GridModel.Size, GridModel.Size];

        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                var cell = CreateCell(_owner, _board[y * GridModel.Size + x]);
                cells[x, y] = cell;
                grid.Children.Add(cell);
            }
        }

        var panel = new Panel(grid, cells);

        if (_cellClicked is not null)
        {
            panel.CellClicked += _cellClicked;
        }

        return panel;
    }

    private static Button CreateCell(BoardCellOwner owner, CellState state)
    {
        var button = new Button
        {
            Margin = new Thickness(1),
        };

        switch (owner)
        {
            case BoardCellOwner.Player:
                button.Content = state == CellState.Ship ? "X" : state == CellState.Obstacle ? "#" : null;
                button.Background = state == CellState.Ship ? Brushes.SteelBlue : Brushes.Gray;
                button.IsHitTestVisible = false;
                break;
            case BoardCellOwner.Enemy:
                button.Background = Brushes.Gray;
                button.IsEnabled = true;
                break;
            case BoardCellOwner.Placement:
                button.Content = state == CellState.Obstacle ? "#" : null;
                button.Background = state == CellState.Obstacle ? Brushes.Gray : Brushes.LightBlue;
                button.IsEnabled = state != CellState.Obstacle;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(owner), owner, null);
        }

        return button;
    }
}
