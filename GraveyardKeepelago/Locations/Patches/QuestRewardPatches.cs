using System;
using System.Collections.Generic;
using FlowCanvas;
using FlowCanvas.Nodes;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(FlowGraph), "OnGraphStarted")]
public static class FlowGraphOnGraphStartedLocationPatch
{
    private static readonly HashSet<FlowOutput> InstrumentedOutputs = new HashSet<FlowOutput>();

    private static void Postfix(FlowGraph __instance)
    {
        foreach (var sourceNode in __instance.allNodes)
        {
            foreach (var connection in sourceNode.outConnections)
            {
                if (!(connection is BinderConnection binder) ||
                    !(binder.sourcePort is FlowOutput source) ||
                    !(binder.targetPort is FlowInput target) ||
                    !InstrumentedOutputs.Add(source))
                    continue;

                var original = target.pointer;
                var targetNode = target.parent;
                var graphName = __instance.name;
                var targetNodeID = targetNode?.ID ?? -1;
                var targetNodeType = targetNode?.GetType().Name;
                var continuation = targetNode?.GetOutputPort("Out") as FlowOutput;
                int? answerIndex = null;
                if (sourceNode is Flow_MultiAnswer &&
                    source.ID.StartsWith("out_", StringComparison.Ordinal) &&
                    int.TryParse(source.ID.Substring(4), out var parsedAnswerIndex))
                    answerIndex = parsedAnswerIndex;

                source.BindTo((FlowHandler)(flow =>
                {
                    LocationTriggerRuntime.MarkFlowChoice(graphName, sourceNode.ID, answerIndex, ref flow);
                    LocationTriggerRuntime.HandleFlowNode(
                        graphName, targetNodeID, targetNodeType, continuation, original, flow);
                }));
            }
        }
    }
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.OnFinishedCraft))]
public static class GameSaveOnFinishedCraftLocationPatch
{
    private static void Postfix(CraftDefinition craft)
        => LocationTriggerRuntime.HandleCraftCompletion(craft?.id);
}

[HarmonyPatch(typeof(TechUnlockDialogGUI), nameof(TechUnlockDialogGUI.Open),
    typeof(TechDefinition), typeof(GJCommons.VoidDelegate), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
public static class TechUnlockDialogOpenLocationPatch
{
    private static bool Prefix(TechDefinition tech, GJCommons.VoidDelegate on_hide, bool pseudotech)
    {
        if (!pseudotech || !LocationTriggerRuntime.ShouldSuppressCraftReveal(tech?.crafts))
            return true;

        on_hide?.Invoke();
        return false;
    }
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
