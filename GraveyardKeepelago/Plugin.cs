using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using BepInEx;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.GameModifications.Patches;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Locations.Patches;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using GraveyardKeepelago.Utilities;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;
using ReceivedItemManager = GraveyardKeepelago.Items.Receiving.APItemManager;


namespace GraveyardKeepelago
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Graveyard Keeper.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        
        private ILogger _logger;
        private Harmony _harmony;
        private Harmony _saveChecksHarmony;
        private GKArchipelagoClient _archipelago;
        private APItemManager _itemManager;
        private GameSave _newSaveForChecks;
        private GameModificationsPatcher _gameModificationsPatcher;
        private GKLocationChecker _locationChecker;
        private LocationPatcher _locationsPatcher;
        private GKItemRegistry _registry;
        private PlayerActions _playerActions;
        
        private ArchipelagoConnectionInfo APConnectionInfo { get; set; }
        
        /*public ArchipelagoStateDto State { get; set; }*/
        
        public bool IsInGame { get; private set; }
        internal QuestFlowChecks QuestFlowChecks => _archipelago?.QuestFlowChecks;
        internal LocationTriggerService GetLocationTriggers(GameSave save) => _archipelago?.GetLocationTriggers(save);
        internal TechnologyGameChecks GetTechnologyChecks(GameSave save) => _archipelago?.GetTechnologyChecks(save);
        
        private void Awake()
        {
            try
            {
                // cut out the log.info messages the graveyard keeper devs left behind
                //Debug.unityLogger.filterLogType = LogType.Warning;

                // cut out the log.info and log.warning messages the graveyard keeper devs left behind
                Debug.unityLogger.filterLogType = LogType.Error;

                // Plugin startup logic
                Logger.LogInfo($"Loading {PluginInfo.PLUGIN_GUID}...");
                Instance = this;

                _logger = new LogHandler(Logger);

                _harmony = new Harmony(PluginInfo.PLUGIN_NAME);
                //_harmony.PatchAll();

                _archipelago = new GKArchipelagoClient(_logger, _harmony, OnItemReceived);
                // Bind at graph startup, including graphs started before slot initialization.
                _saveChecksHarmony = new Harmony($"{PluginInfo.PLUGIN_GUID}.SaveChecks");
                _saveChecksHarmony.CreateClassProcessor(typeof(QuestFlowPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(NewSaveChecksPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(LoadedSaveChecksPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(ClearSaveChecksPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(StopPlayingSaveChecksPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(PermanentBuffUpdatePatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(BuyTechnologyCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(UnlockTechnologyCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(RecipeBookUseCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(RecipeBookDialogCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(FinishedCraftCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(UnlockCraftCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(CollectWorldItemCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(MoveWorldItemCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(MoveWorldItemToBagCheckPatch)).Patch();
                _saveChecksHarmony.CreateClassProcessor(typeof(PreserveReceivedItemProvenancePatch)).Patch();
                IsInGame = false;
                
                //DumpUtils.DumpAllFlowScripts(_logger);
                //return;

                BasePatch.Initialize(_logger);
                GameModificationsEarlyPatcher.Initialize(_logger, _harmony, _archipelago);
                KeyDispatcher.Initialize(_logger);
                
                var playerActions = new PlayerActions(_logger);
                _playerActions = playerActions;
                PlayerUtilities.Initialize(playerActions);
                LocationHandler.Initialize(_logger, _archipelago);

                _logger.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Plugin Awake failed: {ex}");
            }
        }

        private void Update()
        {
            KeyDispatcher.Update();
            _archipelago?.UpdateSaveChecks();
            if (IsGameReadyForGoals())
                _archipelago?.UpdateGoalCompletion(Time.unscaledTime);
            UpdateReceivedItems();
            UpdatePendingTraps();
        }

        private void UpdatePendingTraps()
        {
            var runner = _archipelago?.PendingTraps;
            if (runner == null) return; // The unversioned path keeps its existing behavior.
            var receiver = _archipelago.ItemPackets;
            var manager = _archipelago.ReceivedItems;
            runner.Update(Time.unscaledTime, () =>
                ReferenceEquals(runner, _archipelago.PendingTraps) &&
                _archipelago.IsCurrentItemSave(receiver, manager) &&
                !manager.HasPendingGrant && IsGameReadyForTraps());
        }

        private static bool IsGameReadyForGoals()
        {
            // Finishing the portal starts a cutscene that disables player input.
            // Goals need a loaded world, but must still report during that cutscene.
            var game = MainGame.me;
            var gui = GUIElements.me;
            return MainGame.game_started && !MainGame.game_starting && !MainGame.disable_all_game &&
                !MainGame.paused && game?.save != null && game.world_root != null && game.player != null &&
                gui != null && !GUIElements.gui_is_initializing && !LoadingGUI.is_shown &&
                (gui.main_menu == null || !gui.main_menu.is_shown);
        }

        private static bool IsGameReadyForTraps()
        {
            if (!IsGameReadyForItems(false) || MainGame.game_starting || MainGame.disable_all_game)
                return false;
            var character = MainGame.me.player_char;
            if (!character.control_enabled || !character.can_be_locally_controlled ||
                character.player_controlled_by_script || !BaseGUI.all_guis_closed)
                return false;
            var gui = GUIElements.me;
            return !gui.speech_bubble.gameObject.activeInHierarchy &&
                !gui.multi_answer.gameObject.activeInHierarchy && !gui.corner_talk.is_shown &&
                !SpeechBubbleGUI.all.Values.Any(bubble => bubble != null && bubble.gameObject.activeInHierarchy) &&
                !gui.cinematic_text.is_shown && !CameraTools.cinematics.IsPlaying &&
                CameraTools.letterbox.Amount <= 0;
        }

        private void UpdateReceivedItems()
        {
            var receiver = _archipelago?.ItemPackets;
            var manager = _archipelago?.ReceivedItems;
            if (receiver == null || manager == null)
                return;

            var history = receiver.History;
            var now = Time.unscaledTime;
            if (manager.HasWork(history, now))
                ProcessReceivedItems(receiver, manager, history, now);
        }

        private void ProcessReceivedItems(ItemPacketReceiver receiver, ReceivedItemManager manager,
            IReadOnlyList<NetworkItem> history, float now)
        {
            var grants = _archipelago.ItemGrantContext;
            manager.Update(history, now, CanApplyNextItem, receiver.GetItemName, grants.Apply, _logger.LogError);

            bool CanApplyNextItem()
            {
                if (!ReferenceEquals(receiver.History, history) || !_archipelago.IsCurrentItemSave(receiver, manager))
                    return false;

                return IsGameReadyForItems(manager.HasPendingGrant);
            }
        }

        private static bool IsGameReadyForItems(bool grantInProgress)
        {
            if (!MainGame.game_started || MainGame.paused)
                return false;

            var player = MainGame.me?.player;
            if (player == null || player.is_dead || player.hp <= 0)
                return false;

            var gui = GUIElements.me;
            if (gui == null || GUIElements.gui_is_initializing || LoadingGUI.is_shown)
                return false;
            if (gui.main_menu != null && gui.main_menu.is_shown)
                return false;

            // A running grant can finish its own cutscene while player input is disabled.
            return grantInProgress || (MainGame.me.player_char != null && GS.IsPlayerEnable());
        }

        private void ResetArchipelago()
        {
            ClearSaveChecks();
            _archipelago.DisconnectPermanently();
            
            _harmony.UnpatchSelf();
            _locationsPatcher?.CleanEvents();
            _gameModificationsPatcher?.CleanEvents();
        }

        private void OnDestroy()
        {
            ClearSaveChecks();
            _saveChecksHarmony?.UnpatchSelf();
        }

        public void EnterGame()
        {
            if (IsInGame)
                return;
            
            if (!ConnectToArchipelago())
                return;

            InitializeAfterConnection();
            
            IsInGame = true;
        }

        private bool ConnectToArchipelago()
        {
            APConnectionInfo = new ArchipelagoConnectionInfo(
                "archipelago.gg", 
                40519, 
                "Player1", 
                true);
            
            /*
            APConnectionInfo = new ArchipelagoConnectionInfo(
                "127.0.0.1",
                38281,
                "Player",
                false
            );
            */
            
            if (APConnectionInfo != null && !_archipelago.IsConnected)
            {
                var connectionResult = _archipelago.ConnectToMultiworld(APConnectionInfo);
                if (!connectionResult.Success)
                {
                    APConnectionInfo = null;
                    _logger.LogError($"Failed to connect to Archipelago: {connectionResult.Message}");
                    return false;
                }
            }
            
            _logger.LogMessage($"Connected to Archipelago as {_archipelago.SlotData.SlotName}");
            return true;
        }

        private void InitializeAfterConnection()
        {
            var registry = new GKItemRegistry();
            var factory = new GKItemFactory(_playerActions);
            factory.BuildAll(registry);
            _registry = registry;

            _locationChecker = new GKLocationChecker(_logger, _archipelago/*, State.LocationsChecked*/);
            _gameModificationsPatcher = new GameModificationsPatcher(_logger, _harmony, _archipelago, _locationChecker,
                registry/*, State*/);

            var trapExecutor = new TrapExecutor(_logger, _archipelago); 

            _itemManager = new APItemManager(_logger, _harmony, _archipelago, _locationChecker, registry, trapExecutor/*, State.ItemsReceived*/);
            
            _locationsPatcher = new LocationPatcher(_logger, _harmony, _archipelago, _locationChecker, registry);
            _gameModificationsPatcher.PatchAllGameLogic();
            if (!_archipelago.SlotData.HasDataVersion)
                _locationsPatcher.ReplaceAllLocationsRewardsWithChecks();
            BindSaveChecks();
        }

        internal void OnNewSaveForChecks()
        {
            _newSaveForChecks = MainGame.me?.save;
            BindSaveChecks();
        }

        internal void BindSaveChecks()
        {
            var save = MainGame.me?.save;
            _archipelago?.BindSaveChecks(save, ReferenceEquals(_newSaveForChecks, save));
        }

        internal void ClearSaveChecks()
        {
            _newSaveForChecks = null;
            _archipelago?.ClearSaveChecks();
        }

        private void OnItemReceived()
        {
            if (!IsInGame || _archipelago?.SlotData?.HasDataVersion != false)
                return;

            _itemManager.ReceiveAllNewItems();
        }
    }
}
