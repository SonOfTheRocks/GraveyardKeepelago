namespace GraveyardKeepelago.Archipelago;

public sealed class ConnectionMenuModel
{
    public ConnectionMenuModel(ConnectionSettings settings)
    {
        ServerAddress = $"{settings.Host}:{settings.Port}";
        SlotName = settings.SlotName;
        Password = settings.Password;
        DeathLink = settings.DeathLink;
    }

    public string ServerAddress { get; set; }
    public string SlotName { get; set; }
    public string Password { get; set; }
    public bool DeathLink { get; set; }

    public bool TryCreateSettings(out ConnectionSettings settings, out string error)
    {
        settings = null;
        if (!TryParseServerAddress(ServerAddress, out var host, out var port, out error))
            return false;

        var candidate = new ConnectionSettings(host, port, SlotName, Password, DeathLink);
        if (!candidate.TryCreateConnectionInfo(out _, out error))
            return false;

        settings = candidate;
        return true;
    }

    public static bool TryParseServerAddress(string serverAddress, out string host, out int port,
        out string error)
    {
        host = string.Empty;
        port = 0;

        var address = serverAddress?.Trim() ?? string.Empty;
        var separator = address.LastIndexOf(':');
        if (separator <= 0 || separator == address.Length - 1)
        {
            error = "Enter the server as host:port, for example archipelago.gg:38281.";
            return false;
        }

        host = address.Substring(0, separator).Trim();
        var portText = address.Substring(separator + 1).Trim();
        if (!int.TryParse(portText, out port))
        {
            error = $"The server port '{portText}' is not a number.";
            return false;
        }

        if (host.StartsWith("[") && host.EndsWith("]"))
            host = host.Substring(1, host.Length - 2);

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "The Archipelago server host must not be empty.";
            return false;
        }

        if (port < 1 || port > 65535)
        {
            error = $"The Archipelago server port must be between 1 and 65535, but was {port}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
