using HarmonyLib;

namespace GraveyardKeepelago.Locations.Patches;

[HarmonyPatch(typeof(GameSave), nameof(GameSave.CreateNewSave))]
internal static class NewSaveChecksPatch
{
    private static void Postfix() => Plugin.Instance?.OnNewSaveForChecks();
}

[HarmonyPatch(typeof(PlayerComponent), nameof(PlayerComponent.SpawnPlayer))]
internal static class LoadedSaveChecksPatch
{
    private static void Postfix(bool is_local_player)
    {
        if (is_local_player)
            Plugin.Instance?.BindSaveChecks();
    }
}

[HarmonyPatch(typeof(SaveSlotsMenuGUI), nameof(SaveSlotsMenuGUI.OnSelectSlotPressed))]
internal static class ClearSaveChecksPatch
{
    private static void Prefix() => Plugin.Instance?.ClearSaveChecks();
}
