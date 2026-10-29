using Avalonia.Controls;
using Avalonia.Media;
using BattleShip.Shared;
using System;
using System.Collections.Generic;

namespace BattleShip.Client;

internal sealed class BoardPanel : Panel
{
    private readonly Button[,] _cells;
    private readonly bool[,] _fired;
    private readonly HashSet<GridPosition> _submarineCells = [];

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

    public bool IsSubmarineCell(int x, int y) =>
        _submarineCells.Contains(new GridPosition(x, y));

    public void SetSubmarineCells(IEnumerable<GridPosition> cells)
    {
        _submarineCells.Clear();
        foreach (var position in cells)
        {
            if (!GridModel.IsInsideBounds(position.X, position.Y))
            {
                continue;
            }

            _submarineCells.Add(position);
            if (!_fired[position.X, position.Y])
            {
                _cells[position.X, position.Y].Content = "S";
                _cells[position.X, position.Y].Background = Brushes.DarkSlateBlue;
            }
        }
    }

    public void SetSelectedMoveCell(GridPosition? position)
    {
        foreach (var cell in _submarineCells)
        {
            if (_fired[cell.X, cell.Y])
            {
                continue;
            }

            _cells[cell.X, cell.Y].Background = position == cell
                ? Brushes.Gold
                : Brushes.DarkSlateBlue;
        }
    }

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
        var view = new PlacedShipView(
            ShipCatalogue.Create(placement.Type),
            new GridPosition(placement.X, placement.Y),
            placement.Orientation);

        foreach (var position in view.Cells)
        {
            var cell = _cells[position.X, position.Y];
            cell.Content = view.Marker;
            cell.Background = placement.Type == ShipType.Submarine
                ? Brushes.DarkSlateBlue
                : view.Fill;
            cell.IsEnabled = false;
        }
    }

    private void HandleCellClicked(int x, int y)
    {
        if (_fired[x, y]) return;
        CellClicked?.Invoke(x, y);
    }
}
