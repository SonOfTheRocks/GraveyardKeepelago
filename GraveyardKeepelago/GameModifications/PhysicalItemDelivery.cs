using System;
using System.IO;

namespace GraveyardKeepelago.GameModifications;

public static class PhysicalItemDelivery
{
    // The receipt caller may commit only once an inventory grant or a native
    // ground drop actually succeeds. Never convert a full inventory into success.
    public static void Deliver(bool inventoryAllowed, Func<bool> addToInventory, Func<bool> drop)
    {
        var wasDropping = PlayerUtilities.IsDroppingItems;
        PlayerUtilities.IsDroppingItems = true;
        try
        {
            if (inventoryAllowed && addToInventory()) return;
            if (!drop()) throw new IOException("The received item could not be delivered to the inventory or dropped. Its receipt was not confirmed.");
        }
        finally { PlayerUtilities.IsDroppingItems = wasDropping; }
    }
}
