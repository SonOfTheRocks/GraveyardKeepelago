using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Utilities;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public void UnlockCraft(string craft_id)
    [HarmonyPatch(typeof(GameSave), "UnlockCraft")]
    public class GameSaveUnlockCraftPatch : BasePatch
    {
        private static bool Prefix(string craft_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("craft_id", craft_id)
            });
            
            var changed = LocationHandler.OnCraftReceive(craft_id);
            if (changed)
                return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;

            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void UnlockTechBranch(int branch_id)
    [HarmonyPatch(typeof(GameSave), "UnlockTechBranch")]
    public class GameSaveUnlockTechBranchPatch : BasePatch
    {
        private static void Prefix(string branch_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("branch_id", branch_id)
            });
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
        
    //public void UnlockTech(string tech_id)
    [HarmonyPatch(typeof(GameSave), "UnlockTech")]
    public class GameSaveUnlockTechPatch : BasePatch
    {
        private static bool Prefix(string tech_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("tech_id", tech_id)
            });
            
            TechDefinition td = GameBalance.me.GetData<TechDefinition>(tech_id);
            if (td == null)
                return MethodPrefix.RUN_ORIGINAL_METHOD;

            FilterUnlockList(td, "crafts", LocationHandler.OnCraftReceive);
            FilterUnlockList(td, "works", LocationHandler.OnWorkReceive);
            FilterUnlockList(td, "phrases", LocationHandler.OnPhraseReceive);
            FilterUnlockList(td, "perks", LocationHandler.OnPerkReceive);
            
            LocationHandler.OnTechUnlockReceive(tech_id);
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }

        private static void FilterUnlockList(TechDefinition td, string fieldName, Func<string, bool> handler)
        {
            var list = Traverse.Create(td).Field<List<string>>(fieldName).Value;
            if (list == null || list.Count == 0)
                return;

            var filtered = list.Where(id => !handler(id)).ToList();
            Traverse.Create(td).Field(fieldName).SetValue(filtered);
        }
    }
    
    //public void RevealHiddenTech(string tech_id)
    [HarmonyPatch(typeof(GameSave), "RevealHiddenTech")]
    public class GameSaveRevealHiddenTechPatch : BasePatch
    {
        private static void Prefix(string tech_id,  MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("tech_id", tech_id)
            });
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void UnlockPerk(string perk_id)
    [HarmonyPatch(typeof(GameSave), "UnlockPerk")]
    public class GameSaveUnlockPerkPatch : BasePatch
    {
        private static bool Prefix(string perk_id, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("perk_id", perk_id)
            });

            // We already get unlockPerk via UnlockTech
            //var changed = LocationHandler.OnPerkReceive(perk_id);
            //if (changed)
            //    return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;

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
        private static void Prefix(List<string> from, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("from", from.JoinToString())
            });
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void SetTaskState(string npc_id, string task_id, KnownNPC.TaskState.State state, System.Action on_finished = null)
}
