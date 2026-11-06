using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.UI;

public static class TextInputShortcutPolicy
{
    // GameKey identities also cover user-rebound keys. UIInput still receives raw text events.
    public static bool Block(bool editing, string key) => editing && key is
        nameof(GameKey.Inventory) or nameof(GameKey.Techs) or nameof(GameKey.KnownNPCs) or nameof(GameKey.Map) or
        nameof(GameKey.GameGUI) or nameof(GameKey.PrevTab) or nameof(GameKey.NextTab) or
        nameof(GameKey.PrevSubTab) or nameof(GameKey.NextSubTub);
}

[HarmonyPatch(typeof(LazyInput), nameof(LazyInput.GetKeyDown), typeof(GameKey))]
internal static class TextInputShortcutPatch
{
    private static bool Prefix(GameKey key, ref bool __result)
    {
        if (!TextInputShortcutPolicy.Block(UIInput.selection != null || JournalGUI.Editing, key.ToString())) return true;
        __result = false;
        return false;
    }
}

