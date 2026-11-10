using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.Patches;
using GraveyardKeepelago.Locations.Patches;
using HarmonyLib;
using GraveyardKeepelago.Items.Traps;
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
            _harmony.CreateClassProcessor(typeof(UI.NativeFormKeyboardFocusPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(UI.TrapQualityBadgePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterUpdatePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterCombatPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterDeathPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterTeleportPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterSavePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(TrapMonsterZoneExitPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(SaveSlotListShowPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(SaveSlotListSelectPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(SaveSlotListHoverPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(SaveSlotListOutPatch)).Patch();
            PatchFlowDispatch();
            AddSkipIntroCapability();
            _harmony.CreateClassProcessor(typeof(FullEnergySleepPatch)).Patch();
            
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
            _harmony.CreateClassProcessor(typeof(ExistingSaveStartupPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(NewSaveStartupPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(SaveSelectionStartupPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayerSpawnStartupPatch)).Patch();
        }

        private static void PatchFlowDispatch()
        {
            // Instrument connections after FlowCanvas has bound the whole graph but
            // before its start-event nodes execute. Tiny port methods are commonly
            // inlined by the JIT and therefore are not reliable Harmony patch points.
            _harmony.CreateClassProcessor(typeof(FlowGraphOnGraphStartedLocationPatch)).Patch();
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
