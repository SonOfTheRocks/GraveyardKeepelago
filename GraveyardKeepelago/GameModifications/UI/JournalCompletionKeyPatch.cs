using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

[HarmonyPatch]
internal static class JournalCompletionKeyPatch
{
    // NGUI sends Tab to both components; consume it before either can deselect the input.
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(UIInput), "OnKey", new[] { typeof(KeyCode) });
        yield return AccessTools.Method(typeof(UIKeyNavigation), nameof(UIKeyNavigation.OnKey), new[] { typeof(KeyCode) });
    }

    private static bool Prefix(Component __instance, KeyCode key) => !JournalGUI.HandleCompletionKey(__instance.gameObject, key);
}

[HarmonyPatch(typeof(UIInput), nameof(UIInput.ProcessEvent))]
internal static class JournalCompletionEventPatch
{
    private static bool Prefix(UIInput __instance, Event ev, ref bool __result)
    {
        if (ev.type != EventType.KeyDown || !JournalGUI.HandleCompletionKey(__instance.gameObject, ev.keyCode)) return true;
        ev.Use();
        __result = true;
        return false;
    }
}
