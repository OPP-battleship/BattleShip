using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BattleShip.Client;

public sealed class ScoreboardPanelFactory : PanelFactory<Panel>
{
    protected override Panel CreatePanel()
    {
        var yourScoreText = new TextBlock { FontSize = 16 };
        var enemyScoreText = new TextBlock { FontSize = 16 };

        var layout = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
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
}
