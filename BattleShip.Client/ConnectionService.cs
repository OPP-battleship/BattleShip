using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleShip.Client.Observers;
using Microsoft.AspNetCore.SignalR.Client;
using BattleShip.Shared;
namespace BattleShip.Client;

public class ConnectionService
{
    private HubConnection? _connection;
    private readonly List<IGameEventObserver> _observers = [];
    private readonly object _observersLock = new();

    public string? MyConnectionId => _connection?.ConnectionId;

    public void Subscribe(IGameEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_observersLock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    public void Unsubscribe(IGameEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_observersLock)
        {
            _observers.Remove(observer);
        }
    }
    
    public async Task ConnectAsync(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(App.IsDebugMode ? $"{serverUrl}/gamehub?debug=true" : $"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<PlacementStartedMessage>("PlacementStarted", message =>
            Notify(observer => observer.OnPlacementStarted(message)));
        _connection.On<GameStartedMessage>("GameStarted", message =>
            Notify(observer => observer.OnGameStarted(message)));
        _connection.On<GameResumedMessage>("GameResumed", message =>
            Notify(observer => observer.OnGameResumed(message)));
        _connection.On<ShotResultMessage>("ShotResult", message =>
            Notify(observer => observer.OnShotResult(message)));
        _connection.On<OpponentDisconnectedMessage>("OpponentDisconnected", message =>
            Notify(observer => observer.OnOpponentDisconnected(message)));

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

    public Task FireShotAsync(ShotStrategyType strategy, int originX, int originY, ShotOrientation lineOrientation)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("FireShot", new FireShotRequest(strategy, originX, originY, lineOrientation));
    }

    public Task<string> SaveGameAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync<string>("SaveGame");
    }

    private void Notify(Action<IGameEventObserver> notification)
    {
        IGameEventObserver[] observers;

        lock (_observersLock)
        {
            observers = [.. _observers];
        }

        foreach (var observer in observers)
        {
            notification(observer);
        }
    }
}
