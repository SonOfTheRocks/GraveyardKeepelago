using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.GameModifications.UI;

public static class ApItemPresentation
{
    // Matches Archipelago's item-name palette and flag precedence (NetUtils.py).
    public static string Color(ItemFlags flags, bool lightBackground = false) =>
        (flags & ItemFlags.Advancement) != 0 ? (lightBackground ? "79439C" : "AF99EF") :
        (flags & ItemFlags.NeverExclude) != 0 ? "6D8BE8" :
        (flags & ItemFlags.Trap) != 0 ? "FA8072" : "00EEEE";

    // NGUI's [c] preserves the AP color even when the native tooltip title has a tint.
    // Close both scopes so subsequent text and reused labels retain their native colors.
    public static string Name(string name, ItemFlags? flags, bool lightBackground = false) => flags.HasValue
        ? $"[c][{Color(flags.Value, lightBackground)}]{name}[-][/c]" : name;

    // KaitoKid's ReceivedItem omits flags. Match its original indexed network receipt,
    // rather than guessing a classification from the name or a different delivery.
    public static ItemFlags? ReceivedFlags(ReceivedItem received, IReadOnlyList<ItemInfo> history)
    {
        if (received == null || history == null || received.UniqueId < 0 || received.UniqueId >= history.Count)
            return null;
        var original = history[received.UniqueId];
        return original.ItemId == received.ItemId && original.LocationId == received.LocationId &&
               original.Player.Slot == received.PlayerId ? original.Flags : (ItemFlags?)null;
    }
}
