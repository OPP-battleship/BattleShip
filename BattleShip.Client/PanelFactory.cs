using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using BattleShip.Shared;
using System;
using System.Collections.Generic;

namespace BattleShip.Client;

public sealed class PanelFactory
{
    public const string Board = "Board";
    public const string Controls = "Controls";
    public const string Scoreboard = "Scoreboard";
    public const string Ships = "Ships";

    public Panel Create(string panelType, BoardCellOwner? owner = null, CellState[]? board = null,
        Action<int, int>? cellClicked = null)
    {
        switch (panelType)
        {
            case Board:
                if (owner is null)
                {
                    throw new ArgumentNullException(nameof(owner));
                }

                ArgumentNullException.ThrowIfNull(board);
                return CreateBoardPanel(owner.Value, board, cellClicked);
            case Controls:
                return CreateControlsPanel();
            case Scoreboard:
                return CreateScoreboardPanel();
            case Ships:
                return CreateShipPanel();
            default:
                throw new ArgumentException($"Unknown panel type: {panelType}", nameof(panelType));
        }
    }

    private static Panel CreateBoardPanel(BoardCellOwner owner, CellState[] board,
        Action<int, int>? cellClicked)
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
                var cell = CreateCell(owner, board[y * GridModel.Size + x]);
                cells[x, y] = cell;
                grid.Children.Add(cell);
            }
        }

        var panel = new Panel(grid, cells);

        if (cellClicked is not null)
        {
            panel.CellClicked += cellClicked;
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

    private static Panel CreateControlsPanel()
    {
        var shotModeComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Width = 140,
            Items =
            {
                new ComboBoxItem { Content = "Single Shot" },
                new ComboBoxItem { Content = "Line Shot" },
                new ComboBoxItem { Content = "Spread Shot" },
            },
        };

        var lineOrientationComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Width = 140,
            Items =
            {
                new ComboBoxItem { Content = "Horizontal" },
                new ComboBoxItem { Content = "Vertical" },
            },
        };

        var layout = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12,
            Children =
            {
                new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Shot Type", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                        shotModeComboBox,
                    },
                },
                new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Line Direction", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                        lineOrientationComboBox,
                    },
                },
            },
        };

        return new Panel(layout, shotModeComboBox, lineOrientationComboBox);
    }

    private static Panel CreateScoreboardPanel()
    {
        var yourScoreText = new TextBlock { FontSize = 16 };
        var enemyScoreText = new TextBlock { FontSize = 16 };

        var layout = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 24,
            Children =
            {
                new TextBlock { Text = "Scoreboard", FontSize = 18, FontWeight = FontWeight.Bold },
                yourScoreText,
                enemyScoreText,
            },
        };

        return new Panel(layout, yourScoreText, enemyScoreText);
    }

    private static Panel CreateShipPanel()
    {
        var shipButtons = new Dictionary<int, Button>
        {
            [4] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [3] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [2] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
            [1] = new Button { HorizontalAlignment = HorizontalAlignment.Stretch },
        };

        var orientationComboBox = new ComboBox
        {
            SelectedIndex = 0,
            Items =
            {
                new ComboBoxItem { Content = "Horizontal" },
                new ComboBoxItem { Content = "Vertical" },
            },
        };

        var readyButton = new Button
        {
            Content = "Ready",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var layout = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Select a ship" },
                shipButtons[4],
                shipButtons[3],
                shipButtons[2],
                shipButtons[1],
                new TextBlock { Text = "Orientation" },
                orientationComboBox,
                readyButton,
            },
        };

        return new Panel(layout, shipButtons, orientationComboBox, readyButton);
    }
}
