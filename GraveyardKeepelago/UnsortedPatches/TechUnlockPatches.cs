using System.Reflection;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public TechUnlock.TechUnlockData GetData()
    [HarmonyPatch(typeof(TechUnlock), nameof(TechUnlock.GetData))]
    public class TechUnlockGetDataPatch : BasePatch
    {
        private static bool Prefix(TechUnlock __instance, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]{});
            
            if (!__instance.id.StartsWith("ap_"))
                return MethodPrefix.RUN_ORIGINAL_METHOD;
            
            if (Traverse.Create(__instance).Field("_data").GetValue() != null)
                return MethodPrefix.RUN_ORIGINAL_METHOD;

            var data = new TechUnlock.TechUnlockData
            {
                name = "AP Unlock",
                description = "An AP Item unlocking... something",
                sprite = APSpriteLoader.GetSprite("archipelago_32x32"),
            };
            Traverse.Create(__instance).Field("_data").SetValue(data);
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}