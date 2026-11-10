using System;
using System.Collections.Generic;
using System.IO;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using System.Linq;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Items;

namespace GraveyardKeepelago.Progression;

public sealed class ItemReceiptLedger
{
    private readonly SaveProgress _progress;
    private readonly Action _store;
    public ItemReceiptLedger(SaveProgress progress, Action store) { _progress = progress; _store = store; }
    public void RestoreProgressiveStages(GKItemRegistry registry)
    {
        foreach (var group in _progress.Receipts.GroupBy(receipt => receipt.Name))
            if (registry.TryGet(group.Key, out var item) && item is GKProgressiveItem progressive)
                progressive.applyCount = Math.Min(group.Count(), progressive.progressiveIDs.Count);
    }
    public void ApplyHistory(IReadOnlyList<ReceivedItem> history, Func<ReceivedItem, bool> apply)
    {
        _progress.ValidateHistory(history);
        for (var index = _progress.Receipts.Count; index < history.Count; index++)
        {
            var receipt = history[index];
            if (receipt.UniqueId != index) throw new InvalidDataException("AP receipts are out of order.");
            if (!apply(receipt)) throw new InvalidDataException($"Could not apply AP item '{receipt.ItemName}' at index {index}.");
            _progress.Receipts.Add(ProcessedReceipt.From(receipt));
            _store();
        }
    }
}
