using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public class ConnectionSettingsTests
{
    [Fact]
    public void ValidSettings_CreateConnectionInfo()
    {
        var settings = new ConnectionSettings(" 127.0.0.1 ", 38281, " Player1 ", "secret", true);

        var success = settings.TryCreateConnectionInfo(out var connectionInfo, out var error);

        Assert.True(success);
        Assert.Equal(string.Empty, error);
        Assert.Equal("127.0.0.1", connectionInfo.HostUrl);
        Assert.Equal(38281, connectionInfo.Port);
        Assert.Equal("Player1", connectionInfo.SlotName);
        Assert.Equal("secret", connectionInfo.Password);
        Assert.True(connectionInfo.DeathLink);
    }

    [Theory]
    [InlineData("", 38281, "Player1", "server host")]
    [InlineData("localhost", 0, "Player1", "server port")]
    [InlineData("localhost", 65536, "Player1", "server port")]
    [InlineData("localhost", 38281, " ", "slot name")]
    public void InvalidSettings_ReturnActionableError(
        string host,
        int port,
        string slotName,
        string expectedErrorFragment)
    {
        var settings = new ConnectionSettings(host, port, slotName, string.Empty, false);

        var success = settings.TryCreateConnectionInfo(out var connectionInfo, out var error);

        Assert.False(success);
        Assert.Null(connectionInfo);
        Assert.Contains(expectedErrorFragment, error, StringComparison.OrdinalIgnoreCase);
    }
}
