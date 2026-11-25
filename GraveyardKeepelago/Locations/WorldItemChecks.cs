using System;
using GraveyardKeepelago.Items;

namespace GraveyardKeepelago.Locations;

internal static class WorldItemChecks
{
    // Shared by ground collection and chest transfers. Persist before consuming
    // the source, so a failure to store the check leaves the reward available.
    internal static bool TryReplace(LocationTriggerService checks, string itemID, int count,
        bool isWorldAcquisition, bool isReceivedItem, Action consume)
    {
        if (!isWorldAcquisition || count <= 0 || ItemProcessor.IsApplyingItem || isReceivedItem ||
            checks?.HandleWorldItem(itemID) != true)
            return false;

        consume();
        return true;
    }

    internal static bool ContainsItem(Item inventory, Item item)
    {
        if (inventory == null || item == null) return false;
        if (ReferenceEquals(inventory, item)) return true;
        // Bags can be nested. Identity, rather than item ID, identifies ownership.
        if (inventory.inventory == null) return false;
        foreach (var child in inventory.inventory)
            if (ContainsItem(child, item)) return true;
        return false;
    }
}
