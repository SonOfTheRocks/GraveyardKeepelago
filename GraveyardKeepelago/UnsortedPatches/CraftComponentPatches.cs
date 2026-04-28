using System.Reflection;
using GraveyardKeepelago.Goals;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    /*public bool CraftAsPlayer(
    CraftDefinition craft,
    Item try_use_particular_item = null,
    List<string> multiquality_ids = null,
    List<Item> override_needs = null,
    bool ignore_crafts_list = false,
    int amount = 1,
    WorldGameObject other_obj_override = null)
    */
    public class CraftComponentCraftAsPlayerPatch : BasePatch
    {
        public static bool Prefix(CraftDefinition craft, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("craft", craft)
            });
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        public static void Postfix(CraftDefinition craft, MethodBase __originalMethod, ref bool __result)
        {
            LogAfter(__originalMethod);
            
            if (!__result)
                return;
            
            if (craft.id == "craft_barrel" || craft.id == "craft_emitter")
                GoalManager.CheckPortalGoalCompletion();
        }
    }
}