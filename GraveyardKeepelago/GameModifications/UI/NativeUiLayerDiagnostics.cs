using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Keep NGUI's warning and repair intact, but identify which widget owns the mismatch.
[HarmonyPatch(typeof(UIWidget), nameof(UIWidget.CheckLayer))]
internal static class NativeUiLayerDiagnostics
{
    private static readonly HashSet<string> Reported = new();

    private static void Prefix(UIWidget __instance)
    {
        var panel = __instance.panel;
        if (panel == null || panel.gameObject.layer == __instance.gameObject.layer) return;
        var widgetPath = Path(__instance.transform);
        if (!Reported.Add(widgetPath)) return;
        Debug.LogWarning($"[AP UI diagnostics] Widget '{widgetPath}' on layer {__instance.gameObject.layer} " +
            $"belongs to panel '{Path(panel.transform)}' on layer {panel.gameObject.layer}.\n" +
            System.Environment.StackTrace);
    }

    private static string Path(Transform transform)
    {
        var names = new List<string>();
        for (var current = transform; current != null; current = current.parent) names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
