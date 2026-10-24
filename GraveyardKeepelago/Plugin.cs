using System;
using BepInEx;
using BepInEx.Configuration;
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
        private GKItemRegistry _registry;
        private PlayerActions _playerActions;
        private ConfigEntry<string> _serverHost;
        private ConfigEntry<int> _serverPort;
        private ConfigEntry<string> _slotName;
        private ConfigEntry<string> _password;
        private ConfigEntry<bool> _deathLink;
        private bool _connectionAttempted;
        
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

                BindConnectionConfig();
                BasePatch.Initialize(_logger);
                GameModificationsEarlyPatcher.Initialize(_logger, _harmony, _archipelago);
                KeyDispatcher.Initialize(_logger);
                KeyDispatcher.Register(KeyCode.F8, RetryArchipelagoConnection);
                
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
            IsInGame = false;
            _connectionAttempted = false;
        }

        public void EnterGame()
        {
            if (IsInGame || _connectionAttempted)
                return;

            _connectionAttempted = true;
            if (!ConnectToArchipelago())
                return;

            InitializeAfterConnection();
            IsInGame = true;
        }

        private void BindConnectionConfig()
        {
            const string section = "Archipelago";
            _serverHost = Config.Bind(section, "ServerHost", "127.0.0.1",
                "Archipelago server hostname or IP address.");
            _serverPort = Config.Bind(section, "ServerPort", 38281,
                "Archipelago server port.");
            _slotName = Config.Bind(section, "SlotName", "Player1",
                "Slot name used when the multiworld was generated.");
            _password = Config.Bind(section, "Password", string.Empty,
                "Optional Archipelago room password.");
            _deathLink = Config.Bind(section, "DeathLink", false,
                "Whether this client connects with DeathLink enabled.");
        }

        private void RetryArchipelagoConnection()
        {
            if (IsInGame)
                return;

            Config.Reload();
            _connectionAttempted = false;
            _logger.LogInfo("Retrying the Archipelago connection from the reloaded configuration.");
            EnterGame();
        }

        private bool ConnectToArchipelago()
        {
            var settings = new ConnectionSettings(
                _serverHost.Value,
                _serverPort.Value,
                _slotName.Value,
                _password.Value,
                _deathLink.Value);
            if (!settings.TryCreateConnectionInfo(out var connectionInfo, out var validationError))
            {
                _logger.LogError($"Invalid Archipelago configuration: {validationError} Fix the BepInEx config and press F8 to retry.");
                return false;
            }

            APConnectionInfo = connectionInfo;
            var errorMessage = "";
            if (!_archipelago.IsConnected)
            {
                var connectionResult = _archipelago.ConnectToMultiworld(APConnectionInfo);
                errorMessage = connectionResult.Message;
            }

            if (!_archipelago.IsConnected)
            {
                APConnectionInfo = null;
                _logger.LogError($"Failed to connect to Archipelago: {errorMessage}. Fix the BepInEx config or server state and press F8 to retry.");
                return false;
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
            _goalManager = new GoalManager(_logger, _harmony, _archipelago, _locationChecker);
            _gameModificationsPatcher = new GameModificationsPatcher(_logger, _harmony, _archipelago, _locationChecker,
                registry/*, State*/);

            var trapExecutor = new TrapExecutor(_logger, _archipelago); 

            _itemManager = new APItemManager(_logger, _harmony, _archipelago, _locationChecker, registry, trapExecutor/*, State.ItemsReceived*/);
            
            _locationsPatcher = new LocationPatcher(_logger, _harmony, _archipelago, _locationChecker, registry);
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
