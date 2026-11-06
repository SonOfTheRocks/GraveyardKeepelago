using System;
using System.IO;
using HarmonyLib;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.GameModifications.Patches;

[HarmonyPatch(typeof(PlatformSpecific), nameof(PlatformSpecific.LoadGame))]
public static class ExistingSaveStartupPatch
{
    private static void Prefix(ref PlatformSpecific.OnGameLoadedDelegate on_lodaded)
    {
        var original = on_lodaded;
        var previousSlot = MainGame.me.save_slot;
        on_lodaded = save =>
        {
            var guard = Plugin.Instance?.ConnectedSaveGuard();
            try
            {
                if (save == null) throw new InvalidDataException("The selected save could not be loaded.");
                // PlatformSpecific has read the file, but OnLoadSlot has not
                // assigned MainGame.save or restored its world yet.
                SaveStartupGuard.ContinueLoadedSave(guard, save.unlocked_phrases, () => original(save));
            }
            catch (InvalidDataException ex)
            {
                MainGame.me.save_slot = previousSlot;
                Plugin.Instance.RejectSaveStartup(ex.Message);
            }
        };
    }
}

[HarmonyPatch(typeof(GameSave), nameof(GameSave.CreateNewSave))]
public static class NewSaveStartupPatch
{
    private static void Prefix(ref Action on_complete)
    {
        // Capture the explicitly selected server when creating this game. Bind
        // before OnNewGame's first PlatformSpecific.SaveGame, not at first sleep.
        var guard = Plugin.Instance?.ConnectedSaveGuard();
        if (guard == null) return;
        var original = on_complete;
        on_complete = () =>
        {
            try { guard.ContinueNewSave(MainGame.me.save.unlocked_phrases, () => original?.Invoke()); }
            catch (InvalidDataException ex) { Plugin.Instance.RejectSaveStartup(ex.Message); }
        };
    }
}

[HarmonyPatch(typeof(SaveSlotsMenuGUI), nameof(SaveSlotsMenuGUI.OnSelectSlotPressed))]
public static class SaveSelectionStartupPatch
{
    private static void Prefix() => Plugin.Instance?.ClearSaveRuntime();
}

[HarmonyPatch(typeof(PlayerComponent), nameof(PlayerComponent.SpawnPlayer))]
public static class PlayerSpawnStartupPatch
{
    private static void Postfix(bool is_local_player)
    {
        // Native loading initializes quests and starts their graphs after the
        // local player is spawned. Install event recording before those events,
        // rather than reconstructing checks from the final technology list.
        if (is_local_player) Plugin.Instance?.EnterGame();
    }
}
