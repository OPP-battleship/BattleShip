using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BattleShip.Shared;
using System;

namespace BattleShip.Client;

public sealed class BoardPanel : UserControl
{
    private readonly Button[,] _cells;
    private readonly bool[,] _fired = new bool[GridModel.Size, GridModel.Size];
    public event Action<int, int>? CellClicked;

    internal BoardPanel(UniformGrid grid, Button[,] cells)
    {
        _cells = cells;
        Content = grid;

        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                int capturedX = x, capturedY = y;
                _cells[x, y].Click += (_, _) => OnCellClicked(capturedX, capturedY);
            }
        }
    }

    public bool HasBeenFired(int x, int y) => _fired[x, y];

    public void ApplyShot(int x, int y, bool isHit, bool isObstacle)
    {
        _fired[x, y] = true;

        var cell = _cells[x, y];
        cell.Content = isHit || isObstacle ? "X" : "*";
        cell.Background = isObstacle
            ? Brushes.Gray
            : isHit
                ? Brushes.Red
                : Brushes.Gold;
    }

    private void OnCellClicked(int x, int y)
    {
        if (_fired[x, y]) return;
        CellClicked?.Invoke(x, y);
    }
}
