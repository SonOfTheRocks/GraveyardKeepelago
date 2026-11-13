using System;
using System.IO;

namespace GraveyardKeepelago.GameModifications;

internal static class PhysicalItemDelivery
{
    public static void Deliver(string itemID, bool inventoryAllowed, Func<bool> addToInventory, Func<bool> drop)
    {
        var wasDropping = PlayerUtilities.IsDroppingItems;
        PlayerUtilities.IsDroppingItems = true;
        try
        {
            if (inventoryAllowed && addToInventory())
                return;

            if (!drop())
                throw new IOException($"Could not deliver AP item '{itemID}' to the inventory or the ground.");
        }
        finally
        {
            PlayerUtilities.IsDroppingItems = wasDropping;
        }
    }
}
