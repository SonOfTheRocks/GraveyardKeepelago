using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using GraveyardKeepelago.Archipelago;
using KaitoKid.Utilities.Interfaces;
using Moq;
using static GraveyardKeepelago.Tests.ReceivedItemHistoryTests;

namespace GraveyardKeepelago.Tests;

public sealed class ItemPacketReceiverTests
{
    private readonly Mock<IArchipelagoSocketHelper> _socket = new();
    private readonly Mock<IArchipelagoSession> _session = new();

    public ItemPacketReceiverTests()
    {
        _session.SetupGet(session => session.Socket).Returns(_socket.Object);
    }

    [Fact]
    public void RequestsSyncOnceUntilAnotherGapOccurs()
    {
        using var receiver = CreateReceiver();
        Assert.Null(receiver.GetHistory());
        Assert.Null(receiver.GetHistory());
        _socket.Verify(socket => socket.SendPacket(It.IsAny<SyncPacket>()), Times.Once);

        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        Assert.Single(receiver.GetHistory()!);

        Receive(new ReceivedItemsPacket { Index = 3, Items = [Item(4)] });
        Assert.Null(receiver.GetHistory());
        Assert.Null(receiver.GetHistory());
        _socket.Verify(socket => socket.SendPacket(It.IsAny<SyncPacket>()), Times.Exactly(2));
    }

    [Fact]
    public void RejectsAnOlderSnapshotAndUnsubscribesOnDisconnect()
    {
        var receiver = CreateReceiver();
        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        var first = receiver.GetHistory()!;
        Assert.True(receiver.IsCurrentHistory(first));

        Receive(new ReceivedItemsPacket { Index = 1, Items = [Item(2)] });
        Assert.False(receiver.IsCurrentHistory(first));
        var second = receiver.GetHistory()!;
        Assert.Equal(2, second.Count);

        receiver.Dispose();
        Receive(new ReceivedItemsPacket { Index = 2, Items = [Item(3)] });
        Assert.Same(second, receiver.GetHistory());
    }

    private ItemPacketReceiver CreateReceiver()
    {
        var logger = Mock.Of<ILogger>();
        return new ItemPacketReceiver(_session.Object, new SlotData("Player", new(), logger), logger);
    }

    private void Receive(ArchipelagoPacketBase packet)
    {
        _socket.Raise(socket => socket.PacketReceived += null, packet);
    }
}
