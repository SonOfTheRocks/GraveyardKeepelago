using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public class ConnectionLossMonitorTests
{
    [Fact]
    public void OnlyOneNoticePerOutageAndOnlyAfterAnActiveInGameConnection()
    {
        var monitor = new ConnectionLossMonitor();
        Assert.False(monitor.Observe(false, false));
        Assert.False(monitor.Observe(true, false));
        Assert.False(monitor.Observe(true, true));
        Assert.True(monitor.Observe(true, false));
        for (var i = 0; i < 100; i++) Assert.False(monitor.Observe(true, false));
        Assert.False(monitor.Observe(true, true));
        Assert.True(monitor.Observe(true, false));
    }

    [Fact]
    public void LeavingSaveOrChangingSessionDoesNotProduceALossNotice()
    {
        var monitor = new ConnectionLossMonitor();
        monitor.Observe(true, true);
        Assert.False(monitor.Observe(false, false));
        Assert.False(monitor.Observe(true, false));
        Assert.False(monitor.Observe(true, true));
        Assert.False(monitor.Observe(false, true));
        Assert.False(monitor.Observe(true, false));
    }
}
