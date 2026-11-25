using System;
using System.Linq;
using GraveyardKeepelago.Items;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.CollectDrop))]
internal static class CollectWorldItemCheckPatch
{
    private static bool Prefix(DropResGameObject __instance, WorldGameObject player)
    {
        var item = __instance.res;
        var game = MainGame.me;
        return !WorldItemChecks.TryReplace(Plugin.Instance?.GetLocationTriggers(game?.save), item?.id,
            item?.value ?? 0, !__instance.is_collected && player != null && ReferenceEquals(player, game?.player),
            ReceivedItemProvenance.IsReceived(item), () =>
            {
                // Native CollectDrop does these before its pickup animation removes the object.
                WorldMap.OnDropItemRemoved(item);
                __instance.is_collected = true;
                __instance.DestroyLinkedHint();
            });
    }
}

[HarmonyPatch(typeof(MultiInventory), nameof(MultiInventory.MoveItemTo),
    typeof(MultiInventory), typeof(Item), typeof(int), typeof(bool), typeof(bool))]
internal static class MoveWorldItemCheckPatch
{
    private static bool Prefix(MultiInventory __instance, MultiInventory another_inventory,
        Item item, int count, bool use_only_first_from_inventory, bool allow_bag, ref bool __result)
    {
        var destination = another_inventory?.all.FirstOrDefault()?.data;
        var sourceMode = use_only_first_from_inventory
            ? MultiInventory.DestinationType.OnlyFirst : MultiInventory.DestinationType.AllFromFirst;
        return RunOriginal(__instance, destination, item, count, sourceMode,
            moving => another_inventory.CanAddItem(moving, allow_bag),
            use_only_first_from_inventory ? MultiInventory.DestinationType.OnlyFirst : MultiInventory.DestinationType.AllFromLast,
            ref __result);
    }

    internal static bool RunOriginal(MultiInventory source, Item destination, Item item, int count,
        MultiInventory.DestinationType countMode, Func<Item, bool> canAdd,
        MultiInventory.DestinationType removeMode, ref bool result)
    {
        var game = MainGame.me;
        var checks = Plugin.Instance?.GetLocationTriggers(game?.save);
        var playerInventory = game?.player?.data;
        if (item == null || checks == null || !checks.HasWorldItem(item.id) ||
            ItemProcessor.IsApplyingItem || ReceivedItemProvenance.IsReceived(item) ||
            !WorldItemChecks.ContainsItem(playerInventory, destination) ||
            WorldItemChecks.ContainsItem(playerInventory, item) ||
            !(countMode == MultiInventory.DestinationType.OnlyFirst ? source.all.Take(1) : source.all)
                .Any(inv => WorldItemChecks.ContainsItem(inv.data, item)))
            return true;

        var available = source.GetTotalCount(item.id, countMode);
        var movedCount = count == 0 ? item.value : Math.Min(count, available);
        // All six native rewards are single, nonstackable objects. Never remove
        // a different copy of the same ID (which might be a received AP item).
        if (item.value != 1 || movedCount != 1) return true;
        var moving = new Item(item) { value = movedCount, equipped_as = ItemDefinition.EquipmentType.None };
        if (!source.IsEnoughItem(moving) || !canAdd(moving)) return true;
        if (!WorldItemChecks.TryReplace(checks, item.id, movedCount, true, false, () =>
            {
                if (!source.TryRemoveSpecificItemNoCheck(item, removeMode))
                    throw new InvalidOperationException("Could not consume the recorded world reward from its source inventory.");
            })) return true;
        result = true;
        return false;
    }
}

[HarmonyPatch(typeof(MultiInventory), nameof(MultiInventory.MoveItemTo), typeof(Item), typeof(Item), typeof(int))]
internal static class MoveWorldItemToBagCheckPatch
{
    private static bool Prefix(MultiInventory __instance, Item another_inventory, Item item, int count, ref bool __result)
        => MoveWorldItemCheckPatch.RunOriginal(__instance, another_inventory, item, count,
            MultiInventory.DestinationType.AllFromFirst, moving => another_inventory.CanAddItem(moving),
            MultiInventory.DestinationType.AllFromLast, ref __result);
}

[HarmonyPatch(typeof(Item), nameof(Item.AddNotFoldedItem))]
internal static class PreserveReceivedItemProvenancePatch
{
    // The native bag-transfer overload recreates an item from just ID/count here,
    // losing its parameters. Preserve the marked physical grant instead.
    private static bool Prefix(Item __instance, Item item, ref bool __result)
    {
        if (!ReceivedItemProvenance.IsReceived(item)) return true;
        __result = __instance.AddItem(item);
        return false;
    }
}
