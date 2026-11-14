using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Items;

internal enum GrantResult
{
    Retry,
    Pending,
    Applied,
}

// All calls are on the game thread. Only a confirmed prefix contributes progressive stages.
internal sealed class SaveBoundItemDelivery
{
    private readonly SaveProgress _progress;
    private readonly Dictionary<string, int> _counts;
    private ItemReceipt _pendingReceipt;
    private bool _draining;

    public bool HasPendingGrant => _pendingReceipt != null;

    public SaveBoundItemDelivery(SaveProgress progress)
    {
        _progress = progress;
        _counts = progress.Data.Receipts.GroupBy(receipt => receipt.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    public void Deliver(IReadOnlyList<NetworkItem> history, Func<bool> canDeliver, Func<long, string> getName,
        Func<string, int, Action<string>, GrantResult> apply)
    {
        if (_draining || !canDeliver())
            return;

        _draining = true;
        try
        {
            DeliverInOrder(history, canDeliver, getName, apply);
        }
        finally
        {
            _draining = false;
        }
    }

    private void DeliverInOrder(IReadOnlyList<NetworkItem> history, Func<bool> canDeliver, Func<long, string> getName,
        Func<string, int, Action<string>, GrantResult> apply)
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
            var result = apply(name, previousReceipts, id => trapID = id);
            if (trapID != null && (result != GrantResult.Applied || string.IsNullOrWhiteSpace(trapID)))
                throw new InvalidDataException($"Invalid trap acceptance at AP item index {index}.");

            if (result == GrantResult.Pending)
            {
                _pendingReceipt = receipt;
                return;
            }

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
