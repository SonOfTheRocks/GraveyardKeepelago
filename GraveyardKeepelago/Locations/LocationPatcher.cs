using System;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Locations.Patches;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Locations
{
    public class LocationPatcher
    {
        private ILogger _logger;
        private GKArchipelagoClient _archipelago;
        private Harmony _harmony;
        private GKLocationChecker _locationChecker;

        private readonly TechsModifier _techsModifier;

        public LocationPatcher(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago,
            GKLocationChecker locationChecker, GKItemRegistry registry)
        {
            _logger = logger;
            _archipelago = archipelago;
            _harmony = harmony;
            _locationChecker = locationChecker;

            _techsModifier = new TechsModifier(logger, archipelago, registry, locationChecker);
        }

        public ILogger Logger
        {
            set { _logger = value; }
            get { return _logger; }
        }

        public void ReplaceAllLocationsRewardsWithChecks()
        {
            try
            {
                ReplaceQuestRewards();
                ReplaceTechUnlocksWithChecks();
                PatchRelationIncreases();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Failed in {nameof(LocationPatcher)}.{nameof(ReplaceAllLocationsRewardsWithChecks)}. Exception: {ex}");
            }
        }

        public void CleanEvents()
        {
            CleanTechUnlockEvents(); //?
        }

        private void ReplaceQuestRewards()
        {
            _logger.LogWarning("TODO: Handle Quest Rewards");
        }

        private void ReplaceTechUnlocksWithChecks()
        {
            // Changing all techs to have 3 unlocks
            foreach (var tech in GameBalance.me.techs_data)
                _techsModifier.ReplaceTechDefinitionUnlocksWithChecks(tech);
         
            // TODO: Correct Data based in SlotData?   
            // patch TechUnlock.GetData to return correct data (icon, description, ...)
            _harmony.CreateClassProcessor(typeof(TechUnlockGetDataPatch)).Patch();
            
            // patch GameSave.UnlockCraft for sending the check
            _harmony.CreateClassProcessor(typeof(GameSaveUnlockCraftPatch)).Patch();
            // patch GameSave.CopyLists (unlocking multiple TechUnlocks at once e.g. when getting a Tech) for sending the checks
            _harmony.CreateClassProcessor(typeof(GameSaveCopyListsPatch)).Patch();
        }

        private void PatchRelationIncreases()
        {
            return;
            // TODO
            throw new NotImplementedException();
        }

        private void CleanTechUnlockEvents()
        {
            return;
            // TODO
            throw new NotImplementedException();
        }
    }
}