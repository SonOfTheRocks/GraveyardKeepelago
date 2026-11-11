using System;
using FlowCanvas;
using FlowCanvas.Nodes;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(FlowGraph), "OnGraphStarted")]
internal static class QuestFlowPatch
{
    private static void Postfix(FlowGraph __instance)
    {
        var checks = Plugin.Instance?.QuestFlowChecks;
        if (checks == null)
            return;

        var graph = __instance.name;
        foreach (var node in __instance.allNodes)
        {
            foreach (var connection in node.outConnections)
            {
                if (!(connection is BinderConnection binder) || !binder.isActive ||
                    !(binder.sourcePort is FlowOutput source) || !(binder.targetPort is FlowInput target))
                    continue;

                int? answerIndex = null;
                if (node is Flow_MultiAnswer && source.ID.StartsWith("out_", StringComparison.Ordinal) &&
                    int.TryParse(source.ID.Substring(4), out var answer))
                    answerIndex = answer;

                var targetNode = target.parent;
                var continuation = targetNode.GetOutputPort("Out") as FlowOutput;
                var current = source.pointer;
                var handler = checks.Wrap(current, target.pointer, continuation == null ? null : new FlowHandler(continuation.Call),
                    graph, node.ID, answerIndex, targetNode.ID, targetNode.GetType().Name);
                if (handler != current)
                    source.BindTo(handler);
            }
        }
    }
}
