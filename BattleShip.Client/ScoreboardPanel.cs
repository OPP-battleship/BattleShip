using Avalonia.Controls;

namespace BattleShip.Client;

public sealed class ScoreboardPanel : UserControl
{
    private readonly TextBlock _yourScoreText;
    private readonly TextBlock _enemyScoreText;

    private int _yourScore;
    private int _enemyScore;

    internal ScoreboardPanel(Panel layout, TextBlock yourScoreText, TextBlock enemyScoreText)
    {
        _yourScoreText = yourScoreText;
        _enemyScoreText = enemyScoreText;
        Content = layout;
        Refresh();
    }

    public void AddPoint(bool isYou)
    {
        if (isYou) _yourScore++;
        else _enemyScore++;
        Refresh();
    }

    private void Refresh()
    {
        _yourScoreText.Text = $"You: {_yourScore}";
        _enemyScoreText.Text = $"Enemy: {_enemyScore}";
    }
}
