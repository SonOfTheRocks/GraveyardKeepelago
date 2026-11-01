using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public class ManualConnectionSessionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadingOrCreatingSaveWithoutMenuConnectDoesNotActivateAp(bool socketConnected)
    {
        var session = new ManualConnectionSession();
        var applied = 0;
        Assert.False(session.TryEnterSave(socketConnected, () => applied++));
        Assert.Equal(0, applied);
    }

    [Fact]
    public void ExplicitConnectAllowsSaveInitializationAndItems()
    {
        var session = new ManualConnectionSession();
        var initialized = 0;
        session.ConnectedFromMenu();
        Assert.True(session.TryEnterSave(true, () => initialized++));
        Assert.Equal(1, initialized);
    }

    [Fact]
    public void DisconnectThenLoadDoesNotReactivateAp()
    {
        var session = new ManualConnectionSession();
        session.ConnectedFromMenu();
        session.Clear();
        Assert.False(session.TryEnterSave(true, () => Assert.Fail("Disconnected session activated AP")));
    }

    [Fact]
    public void LostConnectionDoesNotActivateSaveButExplicitReconnectCan()
    {
        var session = new ManualConnectionSession();
        session.ConnectedFromMenu();
        Assert.False(session.TryEnterSave(false, () => Assert.Fail("Disconnected save initialized")));
        session.ConnectedFromMenu();
        var initialized = false;
        Assert.True(session.TryEnterSave(true, () => initialized = true));
        Assert.True(initialized);
    }
}
