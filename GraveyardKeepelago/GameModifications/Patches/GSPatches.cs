using System.Reflection;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public static CustomFlowScript RunFlowScript(string uscript_name, CustomFlowScript.OnFinishedDelegate on_finished = false)
    [HarmonyPatch(typeof(GS), nameof(GS.RunFlowScript))]
    public class GSRunFlowScriptPatch : BasePatch
    {
        private static bool Prefix(string uscript_name, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("uscript_name", uscript_name)
            });
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}