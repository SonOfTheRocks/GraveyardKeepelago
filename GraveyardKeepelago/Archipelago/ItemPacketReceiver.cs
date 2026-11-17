using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Archipelago;

// Receives item packets for one connection. Game code reads the history on the game thread.
internal sealed class ItemPacketReceiver : IDisposable
{
    private readonly ReceivedItemHistory _history = new();
    private readonly ILogger _logger;
    private int _lastSyncRequest = -1;

    public IArchipelagoSession Session { get; }
    public SlotData Slot { get; }

    public ItemPacketReceiver(IArchipelagoSession session, SlotData slot, ILogger logger)
    {
        Session = session;
        Slot = slot;
        _logger = logger;
        Session.Socket.PacketReceived += Receive;
    }

    public void Dispose()
    {
        Session.Socket.PacketReceived -= Receive;
    }

    public IReadOnlyList<NetworkItem> GetHistory()
    {
        var history = _history.Snapshot(out var syncGeneration);
        if (history != null)
            return history;

        // The server may send no reply if the slot has never received an item.
        if (_lastSyncRequest == syncGeneration)
            return null;

        Session.Socket.SendPacket(new SyncPacket());
        _lastSyncRequest = syncGeneration;
        _ = ResendKnownChecks();
        return null;
    }

    public bool IsCurrentHistory(IReadOnlyList<NetworkItem> history)
    {
        return ReferenceEquals(history, _history.Snapshot(out _));
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
    }

    private async Task ResendKnownChecks()
    {
        if (!Session.Socket.Connected)
            return;

        var locations = Session.Locations;
        var checkedIDs = locations.AllLocationsChecked.Intersect(locations.AllLocations).ToArray();
        if (checkedIDs.Length == 0)
            return;

        try
        {
            await locations.CompleteLocationChecksAsync(checkedIDs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Archipelago check transmission failed. Saved checks can be resent on reconnect: {ex.Message}");
        }
    }
}
