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

    [Fact]
    public void MenuModel_ParsesTheStandardHostAndPortFormat()
    {
        var model = new ConnectionMenuModel(
            new ConnectionSettings("localhost", 38281, "Old Slot", string.Empty, false))
        {
            ServerAddress = " archipelago.gg:45678 ",
            SlotName = " New Slot ",
            Password = "secret",
            DeathLink = true,
        };

        var success = model.TryCreateSettings(out var settings, out var error);

        Assert.True(success);
        Assert.Equal(string.Empty, error);
        Assert.Equal("archipelago.gg", settings.Host);
        Assert.Equal(45678, settings.Port);
        Assert.Equal("New Slot", settings.SlotName);
        Assert.Equal("secret", settings.Password);
        Assert.True(settings.DeathLink);
    }

    [Theory]
    [InlineData("archipelago.gg", "host:port")]
    [InlineData("archipelago.gg:not-a-port", "not a number")]
    [InlineData("archipelago.gg:0", "between 1 and 65535")]
    [InlineData("archipelago.gg:65536", "between 1 and 65535")]
    public void MenuModel_RejectsInvalidServerAddresses(string serverAddress, string expectedErrorFragment)
    {
        var model = new ConnectionMenuModel(
            new ConnectionSettings("localhost", 38281, "Player1", string.Empty, false))
        {
            ServerAddress = serverAddress,
        };

        var success = model.TryCreateSettings(out var settings, out var error);

        Assert.False(success);
        Assert.Null(settings);
        Assert.Contains(expectedErrorFragment, error, StringComparison.OrdinalIgnoreCase);
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
