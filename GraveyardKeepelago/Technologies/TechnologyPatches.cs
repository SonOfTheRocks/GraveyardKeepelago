using System.Collections.Generic;
using HarmonyLib;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Technologies;

[HarmonyPatch(typeof(TechDefinition), nameof(TechDefinition.GetVisibleUnlocksList))]
public static class TechnologyVisibleRewardsPatch
{
    private static bool Prefix(TechDefinition __instance, ref List<TechUnlock> __result)
    {
        var runtime = TechnologyRuntime.Current;
        if (runtime == null || !runtime.IsActive) return true;
        var previews = runtime.GetPreviewUnlocks(__instance.id);
        if (previews == null) return true;
        __result = previews;
        return false;
    }
}

[HarmonyPatch(typeof(TechUnlock), nameof(TechUnlock.GetData))]
public static class TechnologyPreviewDataPatch
{
    private static bool Prefix(TechUnlock __instance, ref TechUnlock.TechUnlockData __result)
    {
        var runtime = TechnologyRuntime.Current;
        return runtime == null || !runtime.IsActive || !runtime.TryGetPreview(__instance, out __result);
    }
}

[HarmonyPatch(typeof(TechUnlock), nameof(TechUnlock.GetTooltip))]
public static class TechnologyPreviewTooltipPatch
{
    private static bool Prefix(TechUnlock __instance, Tooltip tooltip)
    {
        var runtime = TechnologyRuntime.Current;
        if (runtime == null || !runtime.IsActive || !runtime.TryGetPreview(__instance, out var data)) return true;
        if (tooltip != null)
        {
            tooltip.AddData(new BubbleWidgetTextData(data.name,
                UITextStyles.TextStyle.HintTitle, NGUIText.Alignment.Left));
            tooltip.AddData(new BubbleWidgetTextData(data.description, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
        }
        return false;
    }
}

[HarmonyPatch(typeof(CraftDefinition), nameof(CraftDefinition.IsLocked))]
public static class ReceivedRecipeLockPatch
{
    private static void Postfix(CraftDefinition __instance, ref bool __result)
    {
        var runtime = TechnologyRuntime.Current;
        if (runtime?.IsActive == true && runtime.Unlocks.CraftLocked(__instance.id)) __result = true;
    }
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockPerk))]
public static class ReceivedPerkUnlockPatch
{
    private static bool Prefix(string perk_id)
    {
        var runtime = TechnologyRuntime.Current;
        return runtime?.IsActive != true || runtime.CanUnlockPerk(perk_id);
    }
}

[HarmonyPatch(typeof(TechUnlockDialogGUI), nameof(TechUnlockDialogGUI.OpenAsItemsList))]
public static class TechnologyItemDialogContextPatch
{
    private static void Prefix() => TechnologyRuntime.Current?.ClearTechnologyDialog();
}
