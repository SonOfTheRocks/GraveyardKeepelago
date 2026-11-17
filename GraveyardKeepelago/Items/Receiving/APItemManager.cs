using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Items.Receiving;

// All calls are on the game thread. Only a confirmed prefix contributes progressive stages.
internal sealed class APItemManager
{
    private readonly SaveProgress _progress;
    private readonly Func<string, int, IItemGrant> _createGrant;
    private IItemGrant _activeGrant;
    private readonly Dictionary<string, int> _counts;
    private ItemReceipt _pendingReceipt;
    private bool _draining;

    public bool HasPendingGrant => _pendingReceipt != null;

    public APItemManager(SaveProgress progress, Func<string, int, IItemGrant> createGrant)
    {
        _progress = progress;
        _createGrant = createGrant;
        _counts = progress.Data.Receipts.GroupBy(receipt => receipt.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    public void ProcessReceivedItems(IReadOnlyList<NetworkItem> history, Func<bool> canDeliver, Func<long, string> getName,
        Func<IItemGrant, Action<string>, GrantResult> apply)
    {
        if (_draining || !canDeliver())
            return;

        _draining = true;
        try
        {
            ProcessInOrder(history, canDeliver, getName, apply);
        }
        finally
        {
            _draining = false;
        }
    }

    internal void StopActiveGrant()
    {
        var grant = _activeGrant;
        _activeGrant = null;
        _pendingReceipt = null;
        (grant as IDisposable)?.Dispose();
    }

    private void ProcessInOrder(IReadOnlyList<NetworkItem> history, Func<bool> canDeliver, Func<long, string> getName,
        Func<IItemGrant, Action<string>, GrantResult> apply)
    {
        var data = _progress.Data;
        if (data.InterruptedReceipt is int interrupted && !HasPendingGrant)
            throw new InvalidDataException($"The outcome of AP item {interrupted} is uncertain. Load a save from before its delivery attempt.");

        if (history.Count < data.Receipts.Count + (HasPendingGrant ? 1 : 0))
            throw new InvalidDataException("The server item history is shorter than this save's delivery history.");

        for (var index = 0; index < data.Receipts.Count; index++)
            if (!data.Receipts[index].Matches(history[index]))
                throw new InvalidDataException($"The server item history differs from this save at index {index}.");

        for (var index = data.Receipts.Count; index < history.Count && canDeliver(); index++)
        {
            var item = history[index];
            if (HasPendingGrant && !_pendingReceipt.Matches(item))
                throw new InvalidDataException($"The server item history changed during delivery at index {index}.");

            var name = _pendingReceipt?.Name ?? getName(item.Item);
            if (string.IsNullOrWhiteSpace(name))
                return;

            _counts.TryGetValue(name, out var previousReceipts);
            var receipt = _pendingReceipt ?? new ItemReceipt
            {
                Index = index, ItemID = item.Item, LocationID = item.Location, PlayerID = item.Player, Name = name,
            };

            var grant = HasPendingGrant ? _activeGrant : _createGrant(name, previousReceipts);
            if (grant == null)
                return;

            // Native code can throw after changing game state, or save while an async grant is pending.
            // Leave an interruption marker in such a save and require an intact earlier snapshot to retry.
            if (!HasPendingGrant)
            {
                data.InterruptedReceipt = index;
                try
                {
                    _progress.Store();
                }
                catch
                {
                    data.InterruptedReceipt = null;
                    throw;
                }
            }
            _pendingReceipt = null;
            string trapID = null;
            _activeGrant = grant;
            var result = GrantResult.Retry;
            ItemProcessor.ApplyItem(() => result = apply(grant, id =>
                trapID = id ?? throw new InvalidDataException($"Missing trap ID at AP item index {index}.")));
            if (!ReferenceEquals(_activeGrant, grant))
                return;

            if (trapID != null && (result != GrantResult.Applied || string.IsNullOrWhiteSpace(trapID)))
                throw new InvalidDataException($"Invalid trap acceptance at AP item index {index}.");

            if (result == GrantResult.Pending)
            {
                _pendingReceipt = receipt;
                return;
            }

            StopActiveGrant();
            if (result == GrantResult.Retry)
            {
                data.InterruptedReceipt = null;
                try
                {
                    _progress.Store();
                }
                catch
                {
                    data.InterruptedReceipt = index;
                    throw;
                }
                return;
            }

            if (trapID != null)
                data.PendingTraps.Add(trapID);
            data.Receipts.Add(receipt);
            data.InterruptedReceipt = null;
            try
            {
                _progress.Store();
            }
            catch
            {
                data.InterruptedReceipt = index;
                data.Receipts.RemoveAt(index);
                if (trapID != null)
                    data.PendingTraps.RemoveAt(data.PendingTraps.Count - 1);
                throw;
            }
            _counts[name] = previousReceipts + 1;
        }
    }
}
