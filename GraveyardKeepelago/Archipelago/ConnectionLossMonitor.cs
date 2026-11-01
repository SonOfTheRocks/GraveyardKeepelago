namespace GraveyardKeepelago.Archipelago;

// One notification per actual outage; menu visits and repeated frames do not duplicate it.
public sealed class ConnectionLossMonitor
{
    private bool _connected;
    public bool Observe(bool inGame, bool connected)
    {
        var lost = inGame && _connected && !connected;
        _connected = inGame && connected;
        return lost;
    }
}
