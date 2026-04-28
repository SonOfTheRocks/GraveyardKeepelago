using System.Collections.Generic;
using System.Reflection;
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

            var changed = TechsModifier.HandleTechUnlockLocation(craft_id);
            if (changed)
                return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;

            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }

    //private void CopyLists(List<string> from, List<string> to)
    #pragma warning disable HARMONIZE001
    [HarmonyPatch(typeof(GameSave), "CopyLists")]
    #pragma warning enable HARMONIZE001
    public class GameSaveCopyListsPatch : BasePatch
    {
        private static bool Prefix(List<string> from, List<string> to, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("from", from),
                new ("to", to),
            });

            foreach (var input in from)
            {
                var changed = TechsModifier.HandleTechUnlockLocation(input);
                if (changed)
                    continue;
                
                // normal method behavior
                string str2;
                if ((str2 = input)[0] == '@')
                    str2 = str2.Substring(1);
                if (!to.Contains(str2))
                    to.Add(str2);
            }

            return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}