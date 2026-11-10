using System;

namespace GraveyardKeepelago.Archipelago;

// Saved credentials are not permission to connect or activate AP on a save.
public sealed class ManualConnectionSession
{
    private bool _explicitlyConnected;
    public void ConnectedFromMenu() => _explicitlyConnected = true;
    public void Clear() => _explicitlyConnected = false;

    public bool TryEnterSave(bool connected, Action initialize)
    {
        if (!_explicitlyConnected || !connected) return false;
        initialize();
        return true;
    }
}
