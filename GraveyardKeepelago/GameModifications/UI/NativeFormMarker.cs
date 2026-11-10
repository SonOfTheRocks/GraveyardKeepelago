using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeFormMarker : MonoBehaviour { }

// The game's dialog shortcuts do not test input/selector focus. Restrict this guard to our own form.
[HarmonyPatch(typeof(DialogButtonsGUI), "Update")]
internal static class NativeFormKeyboardFocusPatch
{
    private static bool Prefix(DialogButtonsGUI __instance)
    {
        var input = UIInput.selection;
        var selector = UICamera.selectedObject?.GetComponentInParent<NativeBooleanSelectorMarker>();
        if (input == null && selector == null)
            return true;
        var form = input != null ? input.GetComponentInParent<NativeFormMarker>() :
            selector.GetComponentInParent<NativeFormMarker>();
        return form == null || __instance.GetComponentInParent<DialogGUI>() != GUIElements.me?.dialog;
    }
}
