using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Archipelago;

public sealed class ConnectionSettings
{
    public ConnectionSettings(string host, int port, string slotName, string password, bool deathLink)
    {
        Host = host?.Trim();
        Port = port;
        SlotName = slotName?.Trim();
        Password = password ?? string.Empty;
        DeathLink = deathLink;
    }

    public string Host { get; }
    public int Port { get; }
    public string SlotName { get; }
    public string Password { get; }
    public bool DeathLink { get; }

    public bool TryCreateConnectionInfo(out ArchipelagoConnectionInfo connectionInfo, out string error)
    {
        connectionInfo = null;

        if (string.IsNullOrWhiteSpace(Host))
        {
            error = "The Archipelago server host must not be empty.";
            return false;
        }

        if (Port < 1 || Port > 65535)
        {
            error = $"The Archipelago server port must be between 1 and 65535, but was {Port}.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SlotName))
        {
            error = "The Archipelago slot name must not be empty.";
            return false;
        }

        connectionInfo = new ArchipelagoConnectionInfo(Host, Port, SlotName, DeathLink, Password);
        error = string.Empty;
        return true;
    }
}
