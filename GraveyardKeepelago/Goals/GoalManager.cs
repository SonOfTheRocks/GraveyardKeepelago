using System;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Goals
{
    public class GoalManager
    {
        private static ILogger _logger;
        private readonly Harmony _harmony;
        private static GKArchipelagoClient _archipelago;
        private LocationChecker _locationChecker;

        public GoalManager(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago,
            LocationChecker locationChecker)
        {
            _logger = logger;
            _harmony = harmony;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
        }

        public void CheckGoalCompletion()
        {
            switch (_archipelago.SlotData.Goal)
            {
                case Goal.Portal:
                    CheckPortalGoalCompletion();
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Goal [{_archipelago.SlotData.Goal}] is not supported in this version of the mod.");
            }
        }

        public void InjectGoalMethods()
        {
            switch (_archipelago.SlotData.Goal)
            {
                case Goal.Portal:
                    InjectPortalGoalMethods();
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Goal [{_archipelago.SlotData.Goal}] is not supported in this version of the mod.");
            }
        }

        private void InjectPortalGoalMethods()
        {
            // it would be better to check for a portal enter or cutscene start, but I couldn't find any
            _harmony.Patch(
                original: AccessTools.Method(typeof(CraftComponent), nameof(CraftComponent.CraftAsPlayer)),
                prefix: new HarmonyMethod(typeof(CraftComponentCraftAsPlayerPatch), nameof(CraftComponentCraftAsPlayerPatch.Prefix)),
                postfix: new HarmonyMethod(typeof(CraftComponentCraftAsPlayerPatch), nameof(CraftComponentCraftAsPlayerPatch.Postfix))
            );
        }

        public static void CheckPortalGoalCompletion()
        {
            if (!_archipelago.IsConnected || _archipelago.SlotData.Goal != Goal.Portal)
                return;

            var completedOneTimeCrafts = MainGame.me.save.completed_one_time_crafts;
            if (!completedOneTimeCrafts.Contains("craft_barrel") || !completedOneTimeCrafts.Contains("craft_emitter"))
                return;

            ReportGoalIfQualified();
        }

        public static void ReportGoalIfQualified()
        {
            _archipelago.ReportGoalCompletion();
        }
    }
}