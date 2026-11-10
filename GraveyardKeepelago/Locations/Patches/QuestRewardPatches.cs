using System;
using System.Collections.Generic;
using FlowCanvas;
using FlowCanvas.Nodes;
using HarmonyLib;
using GraveyardKeepelago.Technologies;
using GraveyardKeepelago.GameModifications.Patches;

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
                    if (TutorialDoorAccess.TryHandle(graphName, targetNode, flow)) return;
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
    private static void Postfix(TechUnlockDialogGUI __instance, TechDefinition tech,
        bool forced_unlock, bool reveal_tech, bool pseudotech)
    {
        var runtime = TechnologyRuntime.Current;
        if (!__instance.is_shown || !forced_unlock || reveal_tech || pseudotech ||
            runtime?.IsActive != true || !runtime.IsPreviewDialog(tech)) return;
        __instance.label_header.text = RewardPreviewPresentation.UnlockTitle;
        __instance.subheader.text = RewardPreviewPresentation.UnlockSubtitle;
        __instance.UpdateAllAnchors();
    }

    private static bool Prefix(TechDefinition tech, GJCommons.VoidDelegate on_hide, bool forced_unlock,
        bool reveal_tech, bool pseudotech)
    {
        var runtime = TechnologyRuntime.Current;
        if (runtime?.IsActive == true) runtime.TrackTechnologyDialog(tech, pseudotech);
        var replacesTechnologyReward = LocationTriggerRuntime.HandleTechnologyDialog(
            tech?.id, forced_unlock, reveal_tech, pseudotech);
        if (replacesTechnologyReward && runtime?.IsActive == true)
            runtime.CompleteQuestTechnology(tech.id);
        var replacesCraftReveal = pseudotech && (LocationTriggerRuntime.ShouldSuppressCraftReveal(tech?.crafts) ||
            runtime?.IsActive == true && runtime.ShouldSuppressCraftReveal(tech?.crafts));
        if (!replacesTechnologyReward && !replacesCraftReveal)
            return true;

        // The vanilla forced-unlock dialog sends the check on OK. Send it above instead,
        // then resume the flow graph without showing the redundant reward window.
        on_hide?.Invoke();
        return false;
    }
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockTech))]
public static class GameSaveUnlockTechLocationPatch
{
    private static bool Prefix(string tech_id)
    {
        var runtime = TechnologyRuntime.Current;
        if (runtime?.IsActive == true && LocationTriggerRuntime.IsApplyingArchipelagoItem)
        {
            // Dormant technology-group grants must never complete research checks.
            runtime.RevealTechnology(tech_id);
            return false;
        }
        if (LocationTriggerRuntime.HandleTechnology(tech_id))
        {
            if (runtime?.IsActive == true) runtime.CompleteQuestTechnology(tech_id);
            return false;
        }
        return runtime?.IsActive != true || !runtime.Research(tech_id);
    }
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
