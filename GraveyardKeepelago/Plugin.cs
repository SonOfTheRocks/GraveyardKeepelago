using System;
using BepInEx;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.GameModifications.Patches;
using GraveyardKeepelago.Goals;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Utilities;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;


namespace GraveyardKeepelago
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Graveyard Keeper.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        
        private ILogger _logger;
        private Harmony _harmony;
        private GKArchipelagoClient _archipelago;
        private APItemManager _itemManager;
        private GameModificationsPatcher _gameModificationsPatcher;
        private GKLocationChecker _locationChecker;
        private LocationPatcher _locationsPatcher;
        private GoalManager _goalManager;
        private GKItemManager _gkItemManager;
        private PlayerActions _playerActions;
        
        private ArchipelagoConnectionInfo APConnectionInfo { get; set; }
        
        /*public ArchipelagoStateDto State { get; set; }*/
        
        public bool IsInGame { get; private set; }
        
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
        }

        private void ResetArchipelago()
        {
            _archipelago.DisconnectPermanently();
            
            _harmony.UnpatchSelf();
            _locationsPatcher?.CleanEvents();
            _gameModificationsPatcher?.CleanEvents();
        }

        public void EnterGame()
        {
            if (IsInGame)
                return;
            
            ConnectToArchipelago();
            InitializeAfterConnection();
            
            IsInGame = true;
        }

        private void ConnectToArchipelago()
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
            
            var errorMessage = "";
            if (APConnectionInfo != null && !_archipelago.IsConnected)
            {
                var connectionResult = _archipelago.ConnectToMultiworld(APConnectionInfo);
                errorMessage = connectionResult.Message;
            }

            if (!_archipelago.IsConnected)
            {
                APConnectionInfo = null;
                _logger.LogError($"Failed to connect to archipelago: {errorMessage}!");
                Console.ReadKey();
                Environment.Exit(0);
            }
            
            _logger.LogMessage($"Connected to Archipelago as {_archipelago.SlotData.SlotName}");
        }

        private void InitializeAfterConnection()
        {
            _gkItemManager = new GKItemManager(_logger, _playerActions);
            _locationChecker = new GKLocationChecker(_logger, _archipelago/*, State.LocationsChecked*/);
            _goalManager = new GoalManager(_logger, _harmony, _archipelago, _locationChecker);
            _gameModificationsPatcher = new GameModificationsPatcher(_logger, _harmony, _archipelago, _locationChecker,
                _gkItemManager/*, State*/);

            var trapExecutor = new TrapExecutor(_logger, _archipelago); 

            _itemManager = new APItemManager(_logger, _harmony, _archipelago, _locationChecker, _gkItemManager, trapExecutor/*, State.ItemsReceived*/);
            
            _locationsPatcher = new LocationPatcher(_logger, _harmony, _archipelago, _locationChecker, _gkItemManager);
            _gameModificationsPatcher.PatchAllGameLogic();
            _locationsPatcher.ReplaceAllLocationsRewardsWithChecks();
            _goalManager.InjectGoalMethods();
        }

        private void OnItemReceived()
        {
            if (!IsInGame || _archipelago == null)
                return;

            _itemManager.ReceiveAllNewItems();
        }
    }
}
