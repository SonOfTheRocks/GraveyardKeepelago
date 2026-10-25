using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeFormMarker : MonoBehaviour { }

// The game's dialog shortcuts do not test UIInput focus. Restrict this guard to our own form.
[HarmonyPatch(typeof(DialogButtonsGUI), "Update")]
internal static class NativeFormKeyboardFocusPatch
{
    private static bool Prefix(DialogButtonsGUI __instance)
    {
        var input = UIInput.selection;
        if (input == null)
            return true;
        var form = input.GetComponentInParent<NativeFormMarker>();
        return form == null || __instance.GetComponentInParent<DialogGUI>() != GUIElements.me?.dialog;
    }
}
