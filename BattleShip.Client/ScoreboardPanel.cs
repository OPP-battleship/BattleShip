using Avalonia.Controls;

namespace BattleShip.Client;

internal sealed class ScoreboardPanel : Panel
{
    private readonly TextBlock _yourScoreText;
    private readonly TextBlock _enemyScoreText;
    private int _yourScore;
    private int _enemyScore;

    internal ScoreboardPanel(Avalonia.Controls.Panel layout, TextBlock yourScoreText, TextBlock enemyScoreText)
        : base(layout)
    {
        _yourScoreText = yourScoreText;
        _enemyScoreText = enemyScoreText;
        RefreshScore();
    }

    public void AddPoint(bool isYou)
    {
        if (isYou) _yourScore++;
        else _enemyScore++;
        RefreshScore();
    }

    private void RefreshScore()
    {
        _yourScoreText.Text = $"You: {_yourScore}";
        _enemyScoreText.Text = $"Enemy: {_enemyScore}";
    }
}
