using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public class ConnectionMenuControllerTests
{
    private bool _connected, _initialized;
    private int _connectCalls, _disconnectCalls;
    private bool _succeed = true;
    private ConnectionSettings? _received;

    private ConnectionMenuController Create() => new(
        new ConnectionSettings("localhost", 38281, "Player1", "secret", false),
        Connect, Disconnect, () => _connected, () => _initialized);

    private bool Connect(ConnectionSettings settings, out string message)
    {
        _connectCalls++;
        _received = settings;
        _connected = _succeed;
        message = _succeed ? "Connected" : "Server unavailable";
        return _succeed;
    }

    private bool Disconnect(out string message)
    {
        _disconnectCalls++;
        if (_succeed)
            _connected = false;
        message = _succeed ? "Disconnected" : "Disconnect failed";
        return _succeed;
    }

    [Theory]
    [InlineData(false, false, true, "Connect")]
    [InlineData(true, false, false, "Disconnect")]
    [InlineData(false, true, false, "Reconnect")]
    [InlineData(true, true, false, "Close")]
    public void StateDeterminesEditingAndAction(bool connected, bool initialized, bool editable, string label)
    {
        _connected = connected;
        _initialized = initialized;
        var controller = Create();
        Assert.Equal(editable, controller.Editable);
        Assert.Equal(label, controller.PrimaryLabel);
    }

    [Fact]
    public void ConnectUsesCurrentFormValues()
    {
        var controller = Create();
        controller.Model.ServerAddress = "ap.example:12345";
        controller.Model.SlotName = "Updated";
        controller.Model.Password = "new secret";
        controller.Model.DeathLink = true;
        Assert.True(controller.ExecutePrimary());
        Assert.Equal("ap.example", _received!.Host);
        Assert.Equal(12345, _received.Port);
        Assert.Equal("Updated", _received.SlotName);
        Assert.Equal("new secret", _received.Password);
        Assert.True(_received.DeathLink);
        Assert.False(controller.Editable);
    }

    [Fact]
    public void ValidationErrorPreservesFormAndDoesNotConnect()
    {
        var controller = Create();
        controller.Model.ServerAddress = "invalid";
        controller.Model.Password = "keep this";
        Assert.False(controller.ExecutePrimary());
        Assert.True(controller.HasError);
        Assert.Equal(0, _connectCalls);
        Assert.Equal("invalid", controller.Model.ServerAddress);
        Assert.Equal("keep this", controller.Model.Password);
        controller.Model.ServerAddress = "localhost:38281";
        Assert.True(controller.ExecutePrimary());
        Assert.False(controller.HasError);
    }

    [Fact]
    public void FailedConnectionCanRetryWithoutLosingValues()
    {
        _succeed = false;
        var controller = Create();
        controller.Model.DeathLink = true;
        Assert.False(controller.ExecutePrimary());
        controller.RefreshConnectionStatus();
        Assert.Equal("Server unavailable", controller.Status);
        Assert.True(controller.Editable);
        _succeed = true;
        Assert.True(controller.ExecutePrimary());
        Assert.Equal(2, _connectCalls);
        Assert.Equal("secret", _received!.Password);
        Assert.True(_received.DeathLink);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisconnectUsesDisconnectActionAndReportsFailure(bool succeeds)
    {
        _connected = true;
        _succeed = succeeds;
        var controller = Create();
        Assert.Equal(succeeds, controller.ExecutePrimary());
        Assert.Equal(1, _disconnectCalls);
        Assert.Equal(0, _connectCalls);
        Assert.Equal(!succeeds, controller.HasError);
        Assert.Equal(succeeds, controller.Editable);
    }

    [Fact]
    public void LoadingSaveBeforePrimaryActionPreventsConnectionChanges()
    {
        var controller = Create();
        _initialized = true;
        _connected = true;
        Assert.True(controller.ExecutePrimary());
        Assert.Equal(0, _connectCalls);
        Assert.Equal(0, _disconnectCalls);
    }

    [Fact]
    public void ConnectionLossUnlocksFormAndReconnectClearsError()
    {
        _connected = true;
        var controller = Create();
        controller.RefreshConnectionStatus();
        _connected = false;
        controller.RefreshConnectionStatus();
        Assert.True(controller.HasError);
        Assert.True(controller.Editable);
        Assert.Contains("Connection lost", controller.Status);
        Assert.True(controller.ExecutePrimary());
        Assert.False(controller.HasError);
        Assert.False(controller.Editable);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LostConnectionInLoadedSaveOffersRetryWithoutChangingServerOrUnlockingFields(bool succeeds)
    {
        _initialized = true;
        var retries = 0;
        bool Reconnect(out string message)
        {
            retries++;
            _connected = succeeds;
            message = succeeds ? "Connected" : "Server unavailable";
            return succeeds;
        }
        var controller = new ConnectionMenuController(
            new ConnectionSettings("localhost", 38281, "Player1", "secret", false),
            Connect, Disconnect, () => _connected, () => _initialized, Reconnect);
        controller.Model.ServerAddress = "invalid edited value must not be used";
        Assert.False(controller.Editable);
        Assert.Equal("Reconnect", controller.PrimaryLabel);
        Assert.Equal(succeeds, controller.ExecutePrimary());
        Assert.Equal(1, retries);
        Assert.Equal(0, _connectCalls);
        Assert.Equal(0, _disconnectCalls);
        Assert.Equal(!succeeds, controller.HasError);
        Assert.False(controller.Editable);
        Assert.Equal(succeeds ? "Close" : "Reconnect", controller.PrimaryLabel);
        if (!succeeds)
        {
            Assert.Equal("Server unavailable", controller.Status);
            Assert.False(controller.ExecutePrimary());
            Assert.Equal(2, retries);
        }
    }}



