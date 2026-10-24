using FlowCanvas;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(FlowOutput), nameof(FlowOutput.BindTo), typeof(FlowInput))]
public static class FlowOutputBindToLocationPatch
{
    private static void Postfix(FlowOutput __instance, FlowInput target)
    {
        var original = target.pointer;
        var node = target.parent;
        var graph = node?.graph;
        var graphName = graph == null ? null : graph.name;
        var nodeID = node?.ID ?? -1;
        var nodeType = node?.GetType().Name;
        var continuation = node?.GetOutputPort("Out") as FlowOutput;
        __instance.BindTo((FlowHandler)(flow =>
            LocationTriggerRuntime.HandleFlowNode(graphName, nodeID, nodeType, continuation, original, flow)));
    }
}

[HarmonyPatch(typeof(FlowOutput), nameof(FlowOutput.Call))]
public static class FlowOutputCallLocationPatch
{
    private static void Prefix(FlowOutput __instance, ref Flow f)
        => LocationTriggerRuntime.MarkFlowChoice(__instance, ref f);
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.OnFinishedCraft))]
public static class GameSaveOnFinishedCraftLocationPatch
{
    private static void Postfix(CraftDefinition craft)
        => LocationTriggerRuntime.HandleCraft(craft?.id);
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockTech))]
public static class GameSaveUnlockTechLocationPatch
{
    private static bool Prefix(string tech_id)
        => !LocationTriggerRuntime.HandleTechnology(tech_id);
}

[HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.AddToInventory), typeof(Item))]
public static class WorldGameObjectAddToInventoryLocationPatch
{
    private static void Postfix(WorldGameObject __instance, Item item, bool __result)
    {
        if (!__result || !__instance.is_player || item == null || LocationTriggerRuntime.IsArchipelagoItem(item) ||
            !LocationTriggerRuntime.HandleWorldItem(item.id))
            return;

        __instance.data.RemoveItem(item.id, item.value);
    }
}

[HarmonyPatch(typeof(MultiInventory), nameof(MultiInventory.MoveItemTo),
    typeof(MultiInventory), typeof(Item), typeof(int), typeof(bool), typeof(bool))]
public static class MultiInventoryMoveItemToLocationPatch
{
    private static void Postfix(MultiInventory another_inventory, Item item, int count, bool __result)
    {
        if (!__result || item == null || LocationTriggerRuntime.IsArchipelagoItem(item) ||
            another_inventory == null || MainGame.me?.player == null ||
            another_inventory.all.Count == 0 ||
            !ReferenceEquals(another_inventory.all[0].data, MainGame.me.player.data) ||
            !LocationTriggerRuntime.HandleWorldItem(item.id))
            return;

        var movedCount = count > 0 ? count : item.value;
        another_inventory.RemoveItem(item.id, movedCount, MultiInventory.DestinationType.OnlyFirst);
    }
}
