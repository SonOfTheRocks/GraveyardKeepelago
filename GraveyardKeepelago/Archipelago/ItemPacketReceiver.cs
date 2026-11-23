using System;
using System.Collections.Generic;
using System.IO;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace GraveyardKeepelago.Archipelago;

// Keep the server's indices. MultiClient handles the socket and gap recovery.
internal sealed class ItemPacketReceiver : IDisposable
{
    private readonly ReceivedItemHistory _history = new();

    public IArchipelagoSession Session { get; }
    public SlotData Slot { get; }
    public IReadOnlyList<NetworkItem> History => _history.Snapshot;

    public ItemPacketReceiver(IArchipelagoSession session, SlotData slot)
    {
        Session = session;
        Slot = slot;
        Session.Socket.PacketReceived += Receive;
    }

    public void RequestInitialHistory()
    {
        // The first batch can arrive before the post-login subscription.
        if (History == null)
            Session.Socket.SendPacket(new SyncPacket());
    }

    public void Dispose()
    {
        Session.Socket.PacketReceived -= Receive;
    }

    public string GetItemName(long id)
    {
        var game = Session.ConnectionInfo.Game;
        var name = Session.Items.GetItemName(id, game);
        if (name == null)
            throw new InvalidDataException($"No {game} Data Package name for item ID {id}.");

        return name;
    }

    private void Receive(ArchipelagoPacketBase packet)
    {
        if (packet is ReceivedItemsPacket items)
            _history.Receive(items.Index, items.Items);
        else if (packet is DataPackagePacket)
            _history.RefreshItemNames();
    }
}
