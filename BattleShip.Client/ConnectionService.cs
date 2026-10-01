using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using BattleShip.Shared;
namespace BattleShip.Client;

public class ConnectionService
{
    private HubConnection? _connection;
    public string? MyConnectionId => _connection?.ConnectionId;

    public event Action<PlacementStartedMessage>? PlacementStarted;
    public event Action<GameStartedMessage>? GameStarted;
    public event Action<ShotResultMessage>? ShotResultReceived;
    public event Action<OpponentDisconnectedMessage>? OpponentDisconnected;
    
    public async Task ConnectAsync(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<PlacementStartedMessage>("PlacementStarted", msg => PlacementStarted?.Invoke(msg));
        _connection.On<GameStartedMessage>("GameStarted", msg => GameStarted?.Invoke(msg));
        _connection.On<ShotResultMessage>("ShotResult", msg => ShotResultReceived?.Invoke(msg));
        _connection.On<OpponentDisconnectedMessage>("OpponentDisconnected", msg => OpponentDisconnected?.Invoke(msg));

        await _connection.StartAsync();
    }
    
    public Task FindMatchAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("FindMatch");
    }

    public Task SubmitShipPlacementAsync(string sessionId, IEnumerable<ShipPlacement> ships)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("SubmitShipPlacement", new SubmitShipPlacementRequest(sessionId, ships.ToArray()));
    }

    public Task FireShotAsync(IEnumerable<ShotTarget> targets)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("FireShot", new FireShotRequest(targets.ToArray()));
    }
}