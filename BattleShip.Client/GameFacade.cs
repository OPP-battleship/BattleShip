using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BattleShip.Client.Observers;
using BattleShip.Shared;

namespace BattleShip.Client;

public sealed class GameFacade
{
    private readonly ConnectionService _connectionService;

    public GameFacade(ConnectionService connectionService)
    {
        ArgumentNullException.ThrowIfNull(connectionService);
        _connectionService = connectionService;
    }

    public string? MyConnectionId => _connectionService.MyConnectionId;

    public void Subscribe(IGameEventObserver observer) => _connectionService.Subscribe(observer);

    public void Unsubscribe(IGameEventObserver observer) => _connectionService.Unsubscribe(observer);

    public Task ConnectAsync(string serverUrl) => _connectionService.ConnectAsync(serverUrl);

    public Task FindMatchAsync() => _connectionService.FindMatchAsync();

    public Task SubmitShipPlacementAsync(string sessionId, IEnumerable<ShipPlacement> ships) =>
        _connectionService.SubmitShipPlacementAsync(sessionId, ships);

    public Task FireShotAsync(ShotStrategyType strategy, int originX, int originY, ShotOrientation lineOrientation) =>
        _connectionService.FireShotAsync(strategy, originX, originY, lineOrientation);

    public Task<bool> MoveShipAsync(int fromX, int fromY, int deltaX, int deltaY) =>
        _connectionService.MoveShipAsync(fromX, fromY, deltaX, deltaY);

    public Task<string> SaveGameAsync() => _connectionService.SaveGameAsync();

    public bool TryValidateShipPlacement(
        CellState[]? board,
        IReadOnlyList<ShipPlacement> currentPlacements,
        ShipPlacement candidate,
        out string failureMessage)
    {
        if (board is null)
        {
            failureMessage = "Placement board is not ready yet.";
            return false;
        }

        var tentativePlacements = new List<ShipPlacement>(currentPlacements) { candidate };
        if (ShipPlacementRules.GetSpentPoints(tentativePlacements) > ShipCatalogue.PointBudget)
        {
            failureMessage = "That ship costs more points than you have remaining.";
            return false;
        }

        var candidateGrid = GridModel.FromFlatArray(board);
        if (!candidateGrid.TryPlaceShips(tentativePlacements))
        {
            failureMessage = "That ship does not fit there. Check the board edges, obstacles, and spacing.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }
}
