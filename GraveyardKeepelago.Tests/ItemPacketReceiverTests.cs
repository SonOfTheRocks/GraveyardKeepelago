using System.Reflection;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.DataPackage;
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
    public void RequestsTheInitialBatchIfItWasMissedButDoesNotRequestOrdinaryGapRecovery()
    {
        // Install the actual MultiClient handler before the adapter, as the session factory does.
        var locations = new Mock<ILocationCheckHelper>();
        var helper = (ReceivedItemsHelper)Activator.CreateInstance(typeof(ReceivedItemsHelper),
            BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
            args: [_socket.Object, locations.Object, Mock.Of<IItemInfoResolver>(),
                Mock.Of<IConnectionInfoProvider>(), Mock.Of<IPlayerHelper>()], culture: null)!;

        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        Assert.Equal(1, helper.Index);

        using var receiver = CreateReceiver();
        receiver.RequestInitialHistory();
        _socket.Verify(socket => socket.SendPacket(It.IsAny<SyncPacket>()), Times.Once);

        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        Assert.Single(receiver.History!);
        Receive(new ReceivedItemsPacket { Index = 3, Items = [Item(4)] });

        Assert.Null(receiver.History);
        _socket.Verify(socket => socket.SendPacket(It.IsAny<SyncPacket>()), Times.Exactly(2));
        locations.Verify(checks => checks.CompleteLocationChecks(It.IsAny<long[]>()), Times.Once);

        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1), Item(2), Item(3), Item(4)] });
        Assert.Equal(4, receiver.History!.Count);
    }

    [Fact]
    public void UnsubscribesOnDisconnectAndDoesNotRequestAnAlreadySeenInitialBatch()
    {
        using var receiver = CreateReceiver();
        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        receiver.RequestInitialHistory();
        _socket.Verify(socket => socket.SendPacket(It.IsAny<SyncPacket>()), Times.Never);

        var first = receiver.History;
        Receive(new ReceivedItemsPacket { Index = 1, Items = [Item(2)] });
        var second = receiver.History!;
        Assert.NotSame(first, second);
        Assert.Equal(2, second.Count);

        receiver.Dispose();
        Receive(new ReceivedItemsPacket { Index = 2, Items = [Item(3)] });
        Assert.Same(second, receiver.History);
    }

    [Fact]
    public void DataPackageArrivalMakesExistingItemsEligibleForAnotherNameLookup()
    {
        using var receiver = CreateReceiver();
        Receive(new ReceivedItemsPacket { Index = 0, Items = [Item(1)] });
        var before = receiver.History!;

        Receive(new DataPackagePacket());

        Assert.NotSame(before, receiver.History);
        Assert.Equal(before.ToArray(), receiver.History!.ToArray());
    }

    private ItemPacketReceiver CreateReceiver()
    {
        return new ItemPacketReceiver(_session.Object, new SlotData("Player", new(), Mock.Of<ILogger>()));
    }

    private void Receive(ArchipelagoPacketBase packet)
    {
        _socket.Raise(socket => socket.PacketReceived += null, packet);
    }
}
