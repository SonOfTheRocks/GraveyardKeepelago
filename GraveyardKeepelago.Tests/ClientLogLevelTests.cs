using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using GraveyardKeepelago.Archipelago;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public class ClientLogLevelTests
{
    [Fact]
    public void RawNetworkPacketsAreDebugInsteadOfUserInformation()
    {
        // The real constructor reads its installed cache layout. Reproduce it
        // inside the test output, without loading Unity or a live connection.
        var cache = Path.Combine(AppContext.BaseDirectory, "BepInEx", "plugins", "GraveyardKeepelago", "IdTables");
        Directory.CreateDirectory(cache);
        foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "IdTables"), "*.json"))
            File.Copy(path, Path.Combine(cache, Path.GetFileName(path)), overwrite: true);
        var logger = new Mock<ILogger>();
        var client = new InspectableClient(logger.Object);
        client.Receive(new SyncPacket());
        logger.Verify(log => log.LogDebug(It.Is<string>(message => message.StartsWith("Packet received: "))), Times.Once);
        logger.Verify(log => log.LogInfo(It.Is<string>(message => message.StartsWith("Packet received: "))), Times.Never);
    }

    private sealed class InspectableClient : GKArchipelagoClient
    {
        // This callback does not use Harmony; no runtime patching in unit tests.
        public InspectableClient(ILogger logger) : base(logger, null!, () => { }) { }
        public void Receive(ArchipelagoPacketBase packet) => OnPacketReceived(packet);
    }
}
