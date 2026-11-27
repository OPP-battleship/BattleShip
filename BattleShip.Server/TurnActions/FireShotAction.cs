using BattleShip.Shared;

namespace BattleShip.Server.TurnActions;

/// <summary>Concrete class: fires a shot pattern at the opponent's board.</summary>
internal sealed class FireShotAction : TurnAction
{
    private readonly FireShotRequest _request;
    private readonly List<ShotOutcome> _outcomes = new();
    private bool _shotAreaUpActive;
    private bool _freeShotActive;

    public FireShotAction(FireShotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _request = request;
    }

    public IReadOnlyList<ShotOutcome> Outcomes => _outcomes;

    protected override bool IsRequestValid(TurnContext turn) =>
        Enum.IsDefined(_request.Strategy) &&
        Enum.IsDefined(_request.LineOrientation) &&
        GridModel.IsInsideBounds(_request.OriginX, _request.OriginY) &&
        turn.ActorGrid.GetAvailableShotStrategies().Contains(_request.Strategy);

    protected override bool Perform(TurnContext turn)
    {
        _outcomes.Clear();
        _shotAreaUpActive = turn.ActorGrid.HasShotAreaUp;
        _freeShotActive = turn.ActorGrid.HasFreeShot;

        IShotStrategy strategy = ShotStrategyFactory.Create(_request.Strategy, _request.LineOrientation);
        if (_shotAreaUpActive)
        {
            strategy = new ShotAreaUpShotStrategyAdapter(strategy);
        }

        var seenTargets = new HashSet<ShotTarget>();
        foreach (var target in strategy.GetTargets(_request.OriginX, _request.OriginY))
        {
            if (!seenTargets.Add(target) || !GridModel.IsInsideBounds(target.X, target.Y))
            {
                continue;
            }

            var cellState = turn.OpponentGrid.GetCell(target.X, target.Y);
            bool isHit = cellState == CellState.Ship;
            bool isObstacle = cellState == CellState.Obstacle;
            if (!turn.OpponentGrid.TryMarkFired(target.X, target.Y, out var powerUp))
            {
                continue;
            }

            PowerUpType? collectedPowerUp = null;
            if (powerUp is not null)
            {
                new PowerUpAdapter(powerUp).ApplyTo(turn.ActorGrid);
                collectedPowerUp = powerUp.GetPowerUpType();
            }

            _outcomes.Add(new ShotOutcome(target.X, target.Y, isHit, isObstacle, collectedPowerUp));
        }

        return _outcomes.Count > 0;
    }

    /// <summary>Hook override: power-ups that were active for this shot are used up.</summary>
    protected override void OnActionCompleted(TurnContext turn)
    {
        if (_shotAreaUpActive)
        {
            turn.ActorGrid.TryConsumeShotAreaUp();
        }

        if (_freeShotActive)
        {
            turn.ActorGrid.TryConsumeFreeShot();
        }
    }

    /// <summary>Hook override: an Extra Turn power-up lets the shooter go again.</summary>
    protected override bool KeepsTurn(TurnContext turn) => turn.ActorGrid.TryConsumeExtraTurn();
}
