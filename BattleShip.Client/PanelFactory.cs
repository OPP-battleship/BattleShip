using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using BattleShip.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShip.Client;

public static class PanelFactory
{
    public const string Board = "Board";
    public const string Controls = "Controls";
    public const string Scoreboard = "Scoreboard";
    public const string Ships = "Ships";

    public static Panel Create(string panelType, BoardCellOwner? owner = null, CellState[]? board = null,
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

    private static BoardPanel CreateBoardPanel(BoardCellOwner owner, CellState[] board,
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
                var cell = CreateCell(owner, board[y * GridModel.Size + x], cellClicked is not null);
                cells[x, y] = cell;
                grid.Children.Add(cell);
            }
        }

        var panel = new BoardPanel(grid, cells);

        if (cellClicked is not null)
        {
            panel.CellClicked += cellClicked;
        }

        return panel;
    }

    private static Button CreateCell(BoardCellOwner owner, CellState state, bool isInteractive)
    {
        var button = new Button
        {
            Margin = new Thickness(1),
        };

        switch (owner)
        {
            case BoardCellOwner.Player:
                button.Content = state switch
                {
                    CellState.Ship => "X",
                    CellState.Obstacle => "#",
                    CellState.PowerUp => "P",
                    _ => null
                };
                button.Background = state switch
                {
                    CellState.Ship => Brushes.SteelBlue,
                    CellState.PowerUp => Brushes.Gold,
                    _ => Brushes.Gray
                };
                button.IsHitTestVisible = isInteractive;
                break;
            case BoardCellOwner.Enemy:
                button.Background = Brushes.Gray;
                button.IsEnabled = true;
                break;
            case BoardCellOwner.Placement:
                button.Content = state switch
                {
                    CellState.Obstacle => "#",
                    CellState.PowerUp => "P",
                    _ => null
                };
                button.Background = state switch
                {
                    CellState.Obstacle => Brushes.Gray,
                    CellState.PowerUp => Brushes.Gold,
                    _ => Brushes.LightBlue
                };
                button.IsEnabled = state == CellState.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(owner), owner, null);
        }

        return button;
    }

    private static ControlsPanel CreateControlsPanel()
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
                new ComboBoxItem { Content = "Ring Shot" },
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
        var moveSubmarineButton = new Button
        {
            Content = "Move Submarine",
            IsEnabled = false,
            Width = 180,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom
        };
        var layout = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 8,
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
                moveSubmarineButton,
            },
        };

        return new ControlsPanel(layout, shotModeComboBox, lineOrientationComboBox, moveSubmarineButton);
    }

    private static ScoreboardPanel CreateScoreboardPanel()
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

        return new ScoreboardPanel(layout, yourScoreText, enemyScoreText);
    }

    private static ShipPanel CreateShipPanel()
    {
        var shipButtons = ShipCatalogue.AllTypes.ToDictionary(
            type => type,
            _ => new Button { HorizontalAlignment = HorizontalAlignment.Stretch });

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

        var undoButton = new Button
        {
            Content = "Undo",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var pointsText = new TextBlock();

        var layout = new StackPanel { Spacing = 8 };
        layout.Children.Add(new TextBlock { Text = "Select ships (duplicates allowed)" });
        foreach (var button in shipButtons.Values)
        {
            layout.Children.Add(button);
        }

        layout.Children.Add(new TextBlock { Text = "Orientation" });
        layout.Children.Add(orientationComboBox);
        layout.Children.Add(pointsText);
        layout.Children.Add(readyButton);
        layout.Children.Add(undoButton);

        return new ShipPanel(layout, shipButtons, orientationComboBox, readyButton, undoButton, pointsText);
    }
}
