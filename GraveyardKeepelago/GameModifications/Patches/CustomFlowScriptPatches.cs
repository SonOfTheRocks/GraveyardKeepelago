using System.Reflection;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public void FireEvent(string event_id)
    [HarmonyPatch(typeof(CustomFlowScript), "FireEvent", typeof(string))]
    public class CustomFlowScriptFireEvent1Patch : BasePatch
    {
        private static bool Prefix(string event_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("event_id", event_id)
            });
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void FireEvent(string event_id, string param)
    [HarmonyPatch(typeof(CustomFlowScript), "FireEvent", typeof(string), typeof(string))]
    public class CustomFlowScriptFireEvent2Patch : BasePatch
    {
        private static bool Prefix(string event_id, string param, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("event_id", event_id),
                new("param", param)
            });
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public static CustomFlowScript Create(GameObject parent_go, FlowGraph g, bool is_global=false, CustomFlowScript.OnFinishedDelegate on_finished=null, string custom_script_name=null)
    [HarmonyPatch(typeof(CustomFlowScript), "Create",
        typeof(GameObject), typeof(string), typeof(bool), typeof(CustomFlowScript.OnFinishedDelegate))]
    public class CustomFlowScriptCreatePatch : BasePatch
    {
        private static bool Prefix(
                GameObject parent_go,
                string script_name,
                bool is_global,
                CustomFlowScript.OnFinishedDelegate on_finished,
                MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("script_name", script_name),
                new("is_global", is_global)
            });
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}