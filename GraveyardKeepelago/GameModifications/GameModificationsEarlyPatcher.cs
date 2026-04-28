using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications
{
    public static class GameModificationsEarlyPatcher
    {
        private static ILogger _logger;
        private static Harmony _harmony;
        
        public static void Initialize(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago)
        {
            _logger = logger;
            _harmony = harmony;
            
            InitializeBasePatch();
            PatchMainMenu();
            AddSkipIntroCapability();
            
            // slow
            AddResourceLoadLogging();
        }

        private static void InitializeBasePatch()
        {
            //BasePatch.Initialize(_logger);
        }

        private static void PatchMainMenu()
        {
            _harmony.CreateClassProcessor(typeof(GameSavePatches.GameSaveGlobalEventsCheckPatch)).Patch();
        }

        private static void AddSkipIntroCapability()
        {
            _harmony.CreateClassProcessor(typeof(IntroShowIntroPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(IntroOnIntroAnimationFinishedPatch)).Patch();
        }

        private static void AddResourceLoadLogging()
        {
            _harmony.CreateClassProcessor(typeof(ResourcesLoadPatch)).Patch();
        }
    }
}