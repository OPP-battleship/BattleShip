using Avalonia.Controls;
using Avalonia.Media;
using BattleShip.Shared;
using System;

namespace BattleShip.Client;

internal sealed class BoardPanel : Panel
{
    private readonly Button[,] _cells;
    private readonly bool[,] _fired;

    public event Action<int, int>? CellClicked;

    internal BoardPanel(Avalonia.Controls.Panel layout, Button[,] cells)
        : base(layout)
    {
        _cells = cells;
        _fired = new bool[GridModel.Size, GridModel.Size];

        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                int capturedX = x, capturedY = y;
                _cells[x, y].Click += (_, _) => HandleCellClicked(capturedX, capturedY);
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

    public void ApplyShipPlacement(ShipPlacement placement)
    {
        foreach (var offset in ShipCatalogue.Create(placement.Type).GetOffsets(placement.Orientation))
        {
            var cell = _cells[placement.X + offset.X, placement.Y + offset.Y];
            cell.Content = "X";
            cell.Background = Brushes.SteelBlue;
            cell.IsEnabled = false;
        }
    }

    private void HandleCellClicked(int x, int y)
    {
        if (_fired[x, y]) return;
        CellClicked?.Invoke(x, y);
    }
}
