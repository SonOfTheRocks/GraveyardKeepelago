using System;
using BepInEx;
using System.Linq;
using System.Threading;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Journal;
using GraveyardKeepelago.GameModifications.UI;
using System.IO;
using GraveyardKeepelago.Technologies;
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
        private GraveyardKeepelago.Items.Traps.TrapMonsterRuntime _trapMonsters;
        private PlayerActions _playerActions;
        private ConfigEntry<string> _serverHost;
        private ConfigEntry<int> _serverPort;
        private ConfigEntry<string> _slotName;
        private ConfigEntry<string> _password;
        private ConfigEntry<bool> _deathLink;
        private bool _connectionAttempted;
        private readonly ManualConnectionSession _manualConnection = new();
        private MainMenuConnectionWindow _connectionWindow;
        private readonly ItemNotificationQueue _itemNotifications = new();
        private readonly ItemNotificationQueue _connectionNotifications = new();
        private readonly ConnectionLossMonitor _connectionLoss = new();
        private ItemNotificationHud _itemNotificationHud;
        private int _itemsPending;
        private float _notificationRetryAfter;
        private float _notificationReadyAfter;
        private GameSave _notificationSave;
        private GameSave _initializedSave;
        private SaveProgress _progress;
        private TechnologyRuntime _technologyRuntime;
        private bool _gameLogicPatched;
        private bool _wasConnected;
        private bool _hasStartedThisSave;
        private JournalService _journal;
        private JournalPlayerTab _journalTab;
        private ConfigEntry<bool> _journalOtherItems;
        private float _journalFlushAfter, _journalRetryAfter;
        
        private ArchipelagoConnectionInfo APConnectionInfo { get; set; }
        
        /*public ArchipelagoStateDto State { get; set; }*/
        
        public bool IsInGame { get; private set; }
        
        private void Awake()
        {
            try
            {
                // Plugin startup logic
                Logger.LogInfo($"Loading {PluginInfo.PLUGIN_GUID}...");
                Instance = this;

                _logger = new LogHandler(Logger);

                _harmony = new Harmony(PluginInfo.PLUGIN_NAME);
                _harmony.CreateClassProcessor(typeof(ConsoleNoisePatch)).Patch();
                //_harmony.PatchAll();

                _archipelago = new GKArchipelagoClient(_logger, _harmony, OnItemReceived);
                _journal = new JournalService(new JournalStore(Path.Combine(Paths.ConfigPath,
                    "GraveyardKeepelago", "Journals")), message => _logger.LogWarning(message));
                _archipelago.Journal = _journal;
                _harmony.CreateClassProcessor(typeof(JournalTypingPatch)).Patch();
                _harmony.CreateClassProcessor(typeof(TextInputShortcutPatch)).Patch();
                _harmony.CreateClassProcessor(typeof(JournalCompletionKeyPatch)).Patch();
                _harmony.CreateClassProcessor(typeof(JournalCompletionEventPatch)).Patch();
                _harmony.CreateClassProcessor(typeof(NativeUiLayerDiagnostics)).Patch();
                IsInGame = false;

                BindConnectionConfig();
                _connectionWindow = new MainMenuConnectionWindow(
                    GetConfiguredConnectionSettings(),
                    ConnectFromMainMenu,
                    DisconnectFromMainMenu,
                    () => _archipelago.IsConnected,
                    () => IsInGame,
                    message => Logger.LogError(message),
                    message => Logger.LogInfo(message), ReconnectFromMenu);
                BasePatch.Initialize(_logger);
                GameModificationsEarlyPatcher.Initialize(_logger, _harmony, _archipelago);
                KeyDispatcher.Initialize(_logger);
                _journalOtherItems = Config.Bind("Journal", "IncludeOtherPlayersItems", false,
                    "Include item transfers between other players in the journal's All filter.");
#if DEBUG
                KeyDispatcher.Register(KeyCode.F9, () =>
                    GameModifications.UI.NativeUiInspector.Inspect(message => Logger.LogDebug(message)));
#endif
                
                var playerActions = new PlayerActions(_logger);
                _playerActions = playerActions;
                PlayerUtilities.Initialize(playerActions);
                LocationHandler.Initialize(_logger, _archipelago);

#if DUMP_MODE
                // Flow export must not depend on every heterogeneous GameBalance
                // field being serializable.
                DumpUtils.DumpAllFlowScripts(_logger);
                var vanillaGameBalance = Resources.Load<GameBalance>("game_data");
                DumpUtils.DumpGameData(
                    _logger,
                    vanillaGameBalance,
                    System.IO.Path.Combine(Paths.BepInExRootPath, "VanillaGameDataExports"));
#endif

                _logger.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Plugin Awake failed: {ex}");
            }
        }

        private void Update()
        {
            _trapMonsters?.Update();
            KeyDispatcher.Update();
            _connectionWindow?.Update();
            UpdateJournal();
            if (_hasStartedThisSave && !MainGame.game_started && GUIElements.me?.main_menu?.is_shown == true)
                ClearSaveRuntime();
            if (!IsInGame || !MainGame.game_started)
            {
                if (_notificationSave != null)
                {
                    ClearItemNotifications();
                    _notificationSave = null;
                }
                return;
            }
            if (MainGame.me?.player == null)
                return;
            _hasStartedThisSave = true;
            if (!ReferenceEquals(_initializedSave, MainGame.me.save))
            {
                EnterGame();
                if (!ReferenceEquals(_initializedSave, MainGame.me.save)) return;
            }
            if (!ReferenceEquals(_notificationSave, MainGame.me.save))
            {
                ClearItemNotifications();
                _notificationSave = MainGame.me.save;
            }
            try
            {
                if (_archipelago.IsConnected)
                {
                    var session = _archipelago.GetSession();
                    _progress.ValidateIdentity(session.RoomState.Seed, session.ConnectionInfo.Team, session.ConnectionInfo.Slot);
                    if (!_wasConnected)
                    {
                        _locationChecker.SendAllLocationChecks();
                        Interlocked.Exchange(ref _itemsPending, 1);
                    }
                }
                _wasConnected = _archipelago.IsConnected;
                if (Interlocked.Exchange(ref _itemsPending, 0) != 0)
                    _itemManager.ReceiveAllNewItems();
                _technologyRuntime?.Update();
                _itemManager?.UpdateTraps();
            }
            catch (Exception ex)
            {
                _logger.LogError($"AP item processing stopped: {ex}");
                ClearSaveRuntime();
                _archipelago.DisconnectPermanently();
                GUIElements.me.dialog.OpenOK(ex.Message, null);
                IsInGame = false;
                _initializedSave = null;
            }
        }

        private void LateUpdate()
        {
            if (_connectionLoss.Observe(IsInGame, _archipelago != null && _archipelago.IsConnected))
            {
                _connectionNotifications.Enqueue(new ItemNotification(
                    "Connection lost. Open Connection in the pause menu to reconnect.",
                    Array.Empty<(string, string)>(), "Archipelago"));
            }
            if (_archipelago != null && _archipelago.IsConnected) _connectionNotifications.Clear();
            if (!IsInGame || Time.unscaledTime < _notificationRetryAfter)
            {
                _itemNotificationHud?.Hide();
                return;
            }
            try
            {
                if (!ItemNotificationWindow.CanShow(allowBusy: true))
                {
                    _itemNotificationHud?.Hide();
                    _notificationReadyAfter = Time.unscaledTime + 0.5f;
                    return;
                }
                if (Time.unscaledTime < _notificationReadyAfter) return;
                var feed = _connectionNotifications.PendingCount > 0 || _connectionNotifications.Visible.Count > 0
                    ? _connectionNotifications : _itemNotifications;
                if (feed.PendingCount == 0 && feed.Visible.Count == 0)
                {
                    _itemNotificationHud?.Hide();
                    return;
                }
                // Construct before dequeuing: a failed native UI mount must not lose a receipt.
                if (_itemNotificationHud == null) _itemNotificationHud = new ItemNotificationHud();
                _itemNotificationHud.Draw(feed.Visible, Time.unscaledDeltaTime);
                feed.Update(Time.unscaledDeltaTime, true);
            }
            catch (Exception ex)
            {
                _itemNotificationHud?.Dispose();
                _itemNotificationHud = null;
                _notificationRetryAfter = Time.unscaledTime + 5;
                _logger.LogError($"Could not display AP item notification: {ex}");
            }
        }

        private void ClearItemNotifications()
        {
            _itemNotifications.Clear();
            _connectionNotifications.Clear();
            _connectionLoss.Observe(false, false);
            _itemNotificationHud?.Dispose();
            _itemNotificationHud = null;
            _notificationReadyAfter = _notificationRetryAfter = 0;
        }
        private void OnDestroy()
        {
            _trapMonsters?.Dispose();
            _journal?.Flush();
            _journalTab?.Dispose();
            _connectionWindow?.Dispose();
            ClearItemNotifications();
            _technologyRuntime?.Dispose();
        }

        private void ResetArchipelago()
        {
            _trapMonsters?.Dispose();
            _trapMonsters = null;
            _playerActions?.SetTrapQueue(null);
            _manualConnection.Clear();
            _archipelago.DisconnectPermanently();
            
            _harmony.UnpatchSelf();
            _gameLogicPatched = false;
            _locationsPatcher?.CleanEvents();
            _gameModificationsPatcher?.CleanEvents();
            ClearItemNotifications();
            IsInGame = false;
            _connectionAttempted = false;
        }

        public void ClearSaveRuntime()
        {
            _trapMonsters?.Dispose();
            _trapMonsters = null;
            _journalTab?.Dispose();
            _journalTab = null;
            _journal?.Flush();
            IsInGame = false;
            LocationTriggerRuntime.Clear();
            _playerActions?.SetTrapQueue(null);
            ClearItemNotifications();
            _technologyRuntime?.Dispose();
            _technologyRuntime = null;
            _initializedSave = null;
            _notificationSave = null;
            _progress = null;
            _itemManager = null;
            _hasStartedThisSave = false;
            _wasConnected = false;
            Interlocked.Exchange(ref _itemsPending, 0);
        }

        public SaveStartupGuard ConnectedSaveGuard()
        {
            if (!_archipelago.IsConnected) return null;
            var session = _archipelago.GetSession();
            return new SaveStartupGuard(session.RoomState.Seed, session.ConnectionInfo.Team, session.ConnectionInfo.Slot);
        }

        public void RejectSaveStartup(string message)
        {
            ClearSaveRuntime();
            _logger.LogWarning($"Save rejected: {message}");
            LoadingGUI.HideImmediate();
            LoadingGUI.ShowBlackBackground(false);
            GUIElements.me.saves.Hide();
            GUIElements.me.dialog.OpenOK(message, () => GUIElements.me.main_menu.Open());
        }

        public void EnterGame()
        {
            if (ReferenceEquals(_initializedSave, MainGame.me?.save) || _connectionAttempted)
                return;

            _connectionAttempted = true;
            try
            {
                _manualConnection.TryEnterSave(_archipelago.IsConnected, () =>
                {
                    _technologyRuntime?.Dispose();
                    _technologyRuntime = null;
                    InitializeAfterConnection();
                    _initializedSave = MainGame.me.save;
                    IsInGame = true;
                    Interlocked.Exchange(ref _itemsPending, 1);
                });
            }
            catch (Exception ex)
            {
                ClearSaveRuntime();
                _logger.LogError($"Cannot initialize AP save: {ex}");
                _manualConnection.Clear();
                _archipelago.DisconnectPermanently();
                GUIElements.me.dialog.OpenOK(ex.Message, null);
            }
            finally { _connectionAttempted = false; }
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

        private ConnectionSettings GetConfiguredConnectionSettings()
            => new ConnectionSettings(
                _serverHost.Value,
                _serverPort.Value,
                _slotName.Value,
                _password.Value,
                _deathLink.Value);

        private void SaveConnectionSettings(ConnectionSettings settings)
        {
            _serverHost.Value = settings.Host;
            _serverPort.Value = settings.Port;
            _slotName.Value = settings.SlotName;
            _password.Value = settings.Password;
            _deathLink.Value = settings.DeathLink;
            Config.Save();
        }

        private bool ConnectFromMainMenu(ConnectionSettings settings, out string message)
        {
            if (IsInGame)
            {
                message = "The connection cannot be changed after loading a save.";
                return false;
            }

            SaveConnectionSettings(settings);
            if (!ConnectToArchipelago(settings, out var errorMessage))
            {
                message = $"Connection failed: {errorMessage}";
                return false;
            }

            _manualConnection.ConnectedFromMenu();
            if (MainGame.game_started && MainGame.me?.player != null)
            {
                EnterGame();
                if (!IsInGame)
                {
                    message = "The connection could not be activated for this save. See the error dialog and client log.";
                    return false;
                }
            }
            message = $"Connected as {_archipelago.SlotData.SlotName}.";
            return true;
        }

        private bool DisconnectFromMainMenu(out string message)
        {
            if (IsInGame)
            {
                message = "The connection cannot be changed after loading a save.";
                return false;
            }

            _manualConnection.Clear();
            _archipelago.DisconnectPermanently();
            APConnectionInfo = null;
            _journal.Flush();
            _journal.CancelConnection();
            _connectionAttempted = false;
            message = "Disconnected";
            return true;
        }

        private bool ReconnectFromMenu(out string message)
        {
            if (!IsInGame)
            {
                message = "Connect from the title screen before loading an AP save.";
                return false;
            }
            try
            {
                if (!_archipelago.MakeSureConnected(0))
                {
                    message = "Reconnection failed. Check the server and try again.";
                    return false;
                }
                var session = _archipelago.GetSession();
                _progress.ValidateIdentity(session.RoomState.Seed, session.ConnectionInfo.Team, session.ConnectionInfo.Slot);
                _wasConnected = false;
                _connectionNotifications.Clear();
                Interlocked.Exchange(ref _itemsPending, 1);
                message = "Connected. Pending checks and items will be synchronized.";
                return true;
            }
            catch (Exception ex)
            {
                _archipelago.DisconnectAndCleanup();
                _logger.LogError($"Could not reconnect Archipelago: {ex}");
                message = ex.Message;
                return false;
            }
        }
        private bool ConnectToArchipelago(ConnectionSettings settings, out string errorMessage)
        {
            if (!settings.TryCreateConnectionInfo(out var connectionInfo, out var validationError))
            {
                _logger.LogError($"Invalid Archipelago configuration: {validationError} Correct the settings in the Connection menu and select Connect.");
                errorMessage = validationError;
                return false;
            }

            APConnectionInfo = connectionInfo;
            errorMessage = string.Empty;
            if (!_archipelago.IsConnected)
            {
                _journal.BeginConnection();
                var connectionResult = _archipelago.ConnectToMultiworld(APConnectionInfo);
                errorMessage = connectionResult.Message;
                if (_archipelago.IsConnected)
                {
                    var session = _archipelago.GetSession();
                    _journal.Bind(new JournalIdentity { Seed = session.RoomState.Seed,
                        Team = session.ConnectionInfo.Team, Slot = session.ConnectionInfo.Slot });
                }
                else _journal.CancelConnection();
            }

            if (!_archipelago.IsConnected)
            {
                APConnectionInfo = null;
                _logger.LogError($"Failed to connect to Archipelago: {errorMessage}. Check the settings and server, then select Connect in the Connection menu.");
                return false;
            }

            _logger.LogMessage($"Connected to Archipelago as {_archipelago.SlotData.SlotName}");
            return true;
        }

        private void InitializeAfterConnection()
        {
            var session = _archipelago.GetSession();
            _progress = SaveProgress.Load(MainGame.me.save.unlocked_phrases, session.RoomState.Seed,
                session.ConnectionInfo.Team, session.ConnectionInfo.Slot);
            var contract = ClientContractLoader.LoadDefault();
            var registry = new GKItemRegistry();
            var factory = new GKItemFactory(_playerActions, contract);
            factory.BuildAll(registry);
            _registry = registry;

            _locationChecker = new GKLocationChecker(_logger, _archipelago, _progress,
                () => _progress.Store(MainGame.me.save.unlocked_phrases));
            _goalManager = new GoalManager(_logger, _harmony, _archipelago, _locationChecker);
            _gameModificationsPatcher = new GameModificationsPatcher(_logger, _harmony, _archipelago, _locationChecker,
                registry/*, State*/);

            _trapMonsters?.Dispose();
            _trapMonsters = new GraveyardKeepelago.Items.Traps.TrapMonsterRuntime(_logger);
            var trapExecutor = new TrapExecutor(_logger, _archipelago); 

            _itemManager = new APItemManager(_logger, _harmony, _archipelago, _locationChecker, registry, trapExecutor,
                _progress, () => _progress.Store(MainGame.me.save.unlocked_phrases));
            _playerActions.SetTrapQueue(_itemManager.QueueTrap);
            
            _locationsPatcher = new LocationPatcher(_logger, _harmony, _archipelago, _locationChecker, registry);
            ClearItemNotifications();
            var notificationGrants = contract.Items.Values
                .ToDictionary(item => item.Name, item => item.Grant);
            _itemManager.ItemProcessor.ItemApplied = (received, item) =>
            {
                var name = received.ItemName;
                notificationGrants.TryGetValue(name, out var grant);
                _itemNotifications.Enqueue(ItemNotification.Create(name, grant, item, received,
                    session.ConnectionInfo.Slot,
                    GameModifications.UI.ApItemPresentation.ReceivedFlags(received, session.Items.AllItemsReceived)));
            };
            if (!_gameLogicPatched) _gameModificationsPatcher.PatchAllGameLogic();
            _locationsPatcher.ReplaceAllLocationsRewardsWithChecks();
            if (!_gameLogicPatched) _goalManager.InjectGoalMethods();
            _gameLogicPatched = true;
            _technologyRuntime = new TechnologyRuntime(MainGame.me.save, _archipelago, _locationChecker,
                contract, TechnologyCatalog.LoadDefault(), _progress, _logger);
            _locationChecker.SendAllLocationChecks();
        }

        private void OnItemReceived()
        {
            // Network callbacks must not touch Unity objects.
            Interlocked.Exchange(ref _itemsPending, 1);
        }

        private void UpdateJournal()
        {
            if (_journal == null) return;
            _journal.Pump();
            if (Time.unscaledTime >= _journalFlushAfter)
            {
                _journal.Flush();
                _journalFlushAfter = Time.unscaledTime + 5;
            }
            if (!IsInGame || !MainGame.game_started || MainGame.me?.player == null ||
                GUIElements.me == null || GUIElements.gui_is_initializing) return;
            if (_journalTab == null && Time.unscaledTime >= _journalRetryAfter)
            {
                try
                {
                    _journalTab = new JournalPlayerTab(_journal, () => _archipelago.IsConnected,
                        message => _archipelago.GetSession().Say(message), () => _journalOtherItems.Value,
                          value => { _journalOtherItems.Value = value; Config.Save(); },
                          () => new JournalCompletion(_registry?.Names ?? Array.Empty<string>(),
                              _archipelago.GetAllLocations().Keys));
                }
                catch (Exception ex)
                {
                    _journalRetryAfter = Time.unscaledTime + 30;
                    _logger.LogWarning($"Could not mount the Archipelago journal: {ex}");
                }
            }
            if (_journalTab == null) return;
            var unread = _journal.History.Unread(_journalOtherItems.Value);
            _journalTab.UpdateIndicator(unread);
        }
    }
}



