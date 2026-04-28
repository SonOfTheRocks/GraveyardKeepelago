using System.Reflection;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public Item(string item_id, int item_value)
    #pragma warning disable HARMONIZE001
    [HarmonyPatch(typeof(Item), "CheckAndInitBagItem")]
    #pragma warning enable HARMONIZE001
    public class ItemCheckAndInitBagItemPatch : BasePatch
    {
        private static bool Prefix(MethodBase __originalMethod)
        {
            //LogBefore(__originalMethod);
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(Item __instance, MethodBase __originalMethod)
        {
            var msg = $"id: {__instance.id}";
            var def = __instance.definition;
            if (def != null)
            {
                msg += $", type: {def.type.ToString()}";
                msg += $", x: {def.bag_size_x}";
                msg += $", y: {def.bag_size_y}";
            }
            msg += $", inventory_size: {__instance.inventory_size}";
            if (__instance.inventory_size > 0)
                Logger.LogInfo(msg);

            //LogAfter(__originalMethod);
        }
    }
}