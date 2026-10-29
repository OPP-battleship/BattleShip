using System;
using System.Collections.Generic;
using BattleShip.Shared;

namespace BattleShip.Client;

internal sealed class ShipPlacementClient
{
    private readonly GameFacade _gameFacade;
    private readonly List<ShipPlacement> _placements = [];
    private CellState[]? _board;

    public ShipPlacementClient(GameFacade gameFacade)
    {
        ArgumentNullException.ThrowIfNull(gameFacade);
        _gameFacade = gameFacade;
    }

    public IReadOnlyList<ShipPlacement> Placements => _placements;

    public void BeginPlacement(CellState[] board)
    {
        ArgumentNullException.ThrowIfNull(board);
        _board = (CellState[])board.Clone();
        _placements.Clear();
    }

    public void Clear()
    {
        _board = null;
        _placements.Clear();
    }

    public bool CanPlaceShip(ShipPlacement placement, out string failureMessage) =>
        _gameFacade.TryValidateShipPlacement(_board, _placements, placement, out failureMessage);

    public void CommitPlacement(ShipPlacement placement) => _placements.Add(placement);

    public bool RevertPlacement(ShipPlacement placement) => _placements.Remove(placement);
}
