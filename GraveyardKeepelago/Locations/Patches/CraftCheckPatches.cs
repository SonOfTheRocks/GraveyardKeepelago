using GraveyardKeepelago.Items;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(GameSave), nameof(GameSave.OnFinishedCraft))]
internal static class FinishedCraftCheckPatch
{
    private static void Postfix(GameSave __instance, CraftDefinition craft)
    {
        if (!ItemProcessor.IsApplyingItem)
            Plugin.Instance?.GetLocationTriggers(__instance)?.HandleCraftCompletion(craft?.id);
    }
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockCraft))]
internal static class UnlockCraftCheckPatch
{
    private static bool Prefix(GameSave __instance, string craft_id)
        => ShouldRunOriginal(Plugin.Instance?.GetLocationTriggers(__instance), craft_id);

    internal static bool ShouldRunOriginal(LocationTriggerService triggers, string craftID)
        => ItemProcessor.IsApplyingItem || triggers?.HandleCraftUnlock(craftID) != true;
}
