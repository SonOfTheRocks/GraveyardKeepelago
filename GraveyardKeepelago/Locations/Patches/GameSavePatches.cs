using System.Collections.Generic;
using System.Reflection;
using GraveyardKeepelago.Technologies;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.Locations.Patches
{
    //public void UnlockCraft(string craft_id)
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.UnlockCraft))]
    public class GameSaveUnlockCraftPatch : BasePatch
    {
        private static bool Prefix(string craft_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("craft_id", craft_id),
            });

            if (LocationTriggerRuntime.HandleCraftUnlock(craft_id))
                return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;

            var runtime = TechnologyRuntime.Current;
            if (runtime?.IsActive == true && !runtime.CanUnlockCraft(craft_id))
                return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;

            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }

}
