using System.Diagnostics;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Deathlink.Patches;
using GraveyardKeepelago.GameModifications.Patches;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications
{
    public class GameModificationsPatcher
    {
        private readonly ILogger _logger;
        private readonly Harmony _harmony;
        private readonly GKArchipelagoClient _archipelago;
        private readonly GKLocationChecker _locationChecker;
        private readonly GKItemRegistry _registry;
        
        public GameModificationsPatcher(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago, GKLocationChecker locationChecker,
            GKItemRegistry registry)
        {
            _harmony = harmony;
            _logger = logger;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
            _registry = registry;
        }

        public void PatchAllGameLogic()
        {
            ModifyDungeonToBeEndless();
            //PatchInventorySize();
            PatchRememberLastAttacker();

            //GUIElements.me.dialog.OpenYesNo
            AddLoggingToGS();
            //AddLoggingToCustomFlowScripts();
            //AddInventoryInitLogging();
            //AddLoggingToQuestSystem();
        }

        public void CleanEvents()
        {
            // Empty for now
        }

        private void ModifyDungeonToBeEndless()
        {
            _harmony.CreateClassProcessor(typeof(SavedDungeonGetSavedDungeonPatch)).Patch();
        }

        private void PatchInventorySize()
        {
            Debug.Assert(MainGame.me != null, "MainGame.me is null");
            Debug.Assert(MainGame.me.player != null, "player is null");

            var traverse = Traverse.Create(MainGame.me.player);
            var field = traverse.Field("_data");
            Debug.Assert(field != null, "_data field not found");

            var playerData = field.GetValue<Item>();
            Debug.Assert(playerData != null, "_data value is null");
            Debug.Assert(playerData.definition != null, "definition is null");
            
            //var playerData = Traverse.Create(MainGame.me.player).Field("_data").GetValue<Item>();
            
            // default inventory size should be 5x4
            Debug.Assert(playerData.definition.bag_size_x == 5 && playerData.definition.bag_size_y == 4);
            Debug.Assert(playerData.inventory_size == 20);

            playerData.definition.bag_size_x = 5;
            playerData.definition.bag_size_y = 8;
            Traverse.Create(playerData).Method("CheckAndInitBagItem").GetValue<bool>();

            // default inventory size should be 5x4
            Debug.Assert(playerData.inventory_size == 40);
        }

        private void PatchRememberLastAttacker()
        {
            _harmony.CreateClassProcessor(typeof(CombatComponentWasHitByPatch)).Patch();
        }

        private void AddLoggingToGS()
        {
            _harmony.CreateClassProcessor(typeof(GSRunFlowScriptPatch)).Patch();
        }

        private void AddLoggingToCustomFlowScripts()
        {
            _harmony.CreateClassProcessor(typeof(CustomFlowScriptFireEvent1Patch)).Patch();
            _harmony.CreateClassProcessor(typeof(CustomFlowScriptFireEvent2Patch)).Patch();
            _harmony.CreateClassProcessor(typeof(CustomFlowScriptCreatePatch)).Patch();
        }

        private void AddInventoryInitLogging()
        {
            _harmony.CreateClassProcessor(typeof(ItemCheckAndInitBagItemPatch)).Patch();
        }

        private void AddLoggingToQuestSystem()
        {
            _harmony.CreateClassProcessor(typeof(QuestSystemStartQuestPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(QuestSystemGiveRewardPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(QuestSystemInitQuestSystemPatch)).Patch();
        }
    }
}