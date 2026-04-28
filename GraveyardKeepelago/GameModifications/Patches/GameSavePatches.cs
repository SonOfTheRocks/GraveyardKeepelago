using System.Reflection;
using GraveyardKeepelago.Utilities;
using HarmonyLib;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    public class GameSavePatches
    {
        //public void GlobalEventsCheck()
        //(only called when a game has finished loading)
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.GlobalEventsCheck))]
        public class GameSaveGlobalEventsCheckPatch : BasePatch
        {
            private static void Prefix(MethodBase __originalMethod)
            {
                LogBefore(__originalMethod, new LogParam[] { });
            }

            private static void Postfix(MethodBase __originalMethod)
            {
                //DumpUtils.DumpPlayerRes(Logger);
                Plugin.Instance.EnterGame();
                LogAfter(__originalMethod);
            }
        }
    }
}