using System.Reflection;
using DungeonGenerator;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public SavedDungeon GetSavedDungeon(int dungeon_level)
    [HarmonyPatch(typeof(SavedDungeonsList), nameof(SavedDungeonsList.GetSavedDungeon))]
    public class SavedDungeonGetSavedDungeonPatch : BasePatch
    {
        public static bool Prefix(int dungeon_level, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("dungeon_level", dungeon_level)
            });
            
            return MethodPrefix.DONT_RUN_ORIGINAL_METHOD;
        }

        public static void Postfix(MethodBase __originalMethod, ref SavedDungeon __result)
        {
            LogAfter(__originalMethod);

            __result = new SavedDungeon();
        }
    }
}