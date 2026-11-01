using System;

namespace GraveyardKeepelago.Archipelago;

public sealed class ConnectionMenuController
{
    public delegate bool ConnectAction(ConnectionSettings settings, out string message);
    public delegate bool DisconnectAction(out string message);
    private readonly ConnectAction _connect;
    private readonly DisconnectAction _disconnect;
    private readonly DisconnectAction _reconnect;
    private readonly Func<bool> _connected;
    private readonly Func<bool> _initialized;
    private bool _wasConnected;

    public ConnectionMenuModel Model { get; }
    public string Status { get; private set; } = "Not connected";
    public bool HasError { get; private set; }
    public bool Locked => _initialized();
    public bool Editable => !Locked && !_connected();
    public string PrimaryLabel => Locked ? (_connected() ? "Close" : "Reconnect") : _connected() ? "Disconnect" : "Connect";

    public ConnectionMenuController(ConnectionSettings settings, ConnectAction connect,
        DisconnectAction disconnect, Func<bool> connected, Func<bool> initialized, DisconnectAction reconnect = null)
    {
        Model = new ConnectionMenuModel(settings);
        _connect = connect;
        _disconnect = disconnect;
        _reconnect = reconnect;
        _connected = connected;
        _initialized = initialized;
    }

    // Return false to reopen the form with the same model and an actionable error.
    public bool ExecutePrimary()
    {
        if (Locked && _connected()) return true;
        bool success;
        string message;
        if (Locked)
        {
            message = "Reconnection is unavailable.";
            success = _reconnect != null && _reconnect(out message);
        }
        else if (_connected())
            success = _disconnect(out message);
        else if (!Model.TryCreateSettings(out var settings, out message))
            success = false;
        else
            success = _connect(settings, out message);
        Status = message;
        HasError = !success;
        _wasConnected = _connected();
        return success;
    }

    public void RefreshConnectionStatus()
    {
        var connected = _connected();
        if (_wasConnected != connected)
        {
            Status = connected ? "Connected" : "Connection lost. Check the server and connect again.";
            HasError = !connected;
        }
        _wasConnected = connected;
    }
}


