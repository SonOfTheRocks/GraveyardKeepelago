using System.Reflection;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public static void LoadGameBalance()
    [HarmonyPatch(typeof(GameBalance), "LoadGameBalance")]
    public class GameBalanceLoadGameBalancePatch : BasePatch
    {
        private static bool Prefix(MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[] {});

            // load resource manually to change it before original method continues normally
            var gb = Resources.Load<GameBalance>("game_data");
            LocationHandler.RandomizeGameBalance(gb);

            // continue original method behaviour
            Traverse.Create(typeof(GameBalance)).Field("_instance").SetValue(gb);
            
            Traverse.Create(gb).Method("CreateIDsCache").GetValue();
            Traverse.Create(gb).Method("CreateItemsBaseNameCache").GetValue();
            Traverse.Create(gb).Method("CreateToolsCache").GetValue();
            Traverse.Create(gb).Method("CreateCraftsCache").GetValue();
            ObjectGroupDefinition.LinkObjectsToGroups();
            // end of original method

            return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}