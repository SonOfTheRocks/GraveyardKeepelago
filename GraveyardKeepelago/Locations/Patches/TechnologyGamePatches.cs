using System;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Technologies;
using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(GameSave), nameof(GameSave.BuyTech))]
internal static class BuyTechnologyCheckPatch
{
    [ThreadStatic] private static (GameSave Save, string ID) _purchase;

    internal static bool IsPurchase(GameSave save, string id)
        => ReferenceEquals(_purchase.Save, save) && _purchase.ID == id;

    private static void Prefix(GameSave __instance, string tech_id, out (GameSave, string) __state)
    {
        __state = _purchase;
        _purchase = (__instance, tech_id);
    }

    private static void Finalizer((GameSave, string) __state) => _purchase = __state;
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockTech))]
internal static class UnlockTechnologyCheckPatch
{
    private static bool Prefix(GameSave __instance, string tech_id)
    {
        var checks = Plugin.Instance?.GetTechnologyChecks(__instance);
        if (checks == null || string.IsNullOrEmpty(tech_id))
            return true;

        var tech = GameBalance.me.GetData<TechDefinition>(tech_id);
        return tech == null || !checks.TryUnlock(tech_id, BuyTechnologyCheckPatch.IsPurchase(__instance, tech_id),
            () => checks.Complete(tech_id, __instance.unlocked_techs, () =>
            {
                if (tech.hidden) __instance.RevealHiddenTech(tech_id);
                if (tech.invisible) __instance.MakeVisibleInvisibleTech(tech_id);
                foreach (var id in checks.GetCrafts(tech.crafts)) __instance.UnlockCraft(id);
                foreach (var id in checks.GetWorks(tech.works))
                    if (!__instance.unlocked_works.Contains(id)) __instance.unlocked_works.Add(id);
                foreach (var id in tech.phrases) __instance.UnlockPhrase(TechnologyGameChecks.Canonical(id));
                foreach (var id in checks.GetPerks(tech.perks)) __instance.UnlockPerk(id);
                if (!string.IsNullOrEmpty(tech.flowscript)) GS.RunFlowScript(tech.flowscript);
                Stats.DesignEvent("Tech:" + tech_id);
            }), ItemProcessor.IsApplyingItem);
    }
}

[HarmonyPatch(typeof(Item), nameof(Item.UseItem))]
internal static class RecipeBookUseCheckPatch
{
    [ThreadStatic] private static (GameSave Save, string ID) _itemUse;

    internal static string GetUsedItem(GameSave save) => ReferenceEquals(_itemUse.Save, save) ? _itemUse.ID : null;

    private static void Prefix(Item __instance, out (GameSave, string) __state)
    {
        __state = _itemUse;
        var save = MainGame.me?.save;
        _itemUse = ItemProcessor.IsApplyingItem || Plugin.Instance?.GetTechnologyChecks(save) == null
            ? default : (save, __instance.id);
    }

    private static void Finalizer((GameSave, string) __state) => _itemUse = __state;
}

[HarmonyPatch(typeof(TechUnlockDialogGUI), nameof(TechUnlockDialogGUI.Open),
    typeof(TechDefinition), typeof(GJCommons.VoidDelegate), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
internal static class RecipeBookDialogCheckPatch
{
    private static void Prefix(TechDefinition tech, bool forced_unlock, bool reveal_tech, bool pseudotech)
    {
        if (!forced_unlock || reveal_tech || pseudotech || tech == null)
            return;

        var save = MainGame.me?.save;
        var itemID = RecipeBookUseCheckPatch.GetUsedItem(save);
        if (itemID != null)
        {
            // Inventory use has already consumed the book. The native OK callback
            // can now repeat UnlockTech without rerunning the technology effects.
            Plugin.Instance?.GetTechnologyChecks(save)?.TryUseRecipeBook(itemID, tech.id, () => save.UnlockTech(tech.id));
        }
    }
}