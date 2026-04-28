using System.Reflection;
using HarmonyLib;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //public void StartQuest(QuestDefinition quest)
    [HarmonyPatch(typeof(QuestSystem), "StartQuest")]
    public class QuestSystemStartQuestPatch : BasePatch
    {
        private static void Prefix(QuestDefinition quest, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("quest.id", quest.id)
            });
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void GiveReward(bool succeed, QuestDefinition quest)
    [HarmonyPatch(typeof(QuestSystem), "GiveReward")]
    public class QuestSystemGiveRewardPatch : BasePatch
    {
        private static void Prefix(QuestDefinition quest, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new ("quest.id", quest.id)
            });
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
    
    //public void InitQuestSystem()
    [HarmonyPatch(typeof(QuestSystem), "InitQuestSystem")]
    public class QuestSystemInitQuestSystemPatch : BasePatch
    {
        private static void Prefix(MethodBase __originalMethod)
        {
            LogBefore(__originalMethod);
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}