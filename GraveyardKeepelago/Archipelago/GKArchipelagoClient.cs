using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Archipelago.ConnectionResults;
using GraveyardKeepelago.Deathlink;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using GraveyardKeepelago.Goals;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.Utilities.Interfaces;
using ReceivedItemManager = GraveyardKeepelago.Items.Receiving.APItemManager;

namespace GraveyardKeepelago.Archipelago
{
    // Most of this class is based on the Stardew Archipelago Mod: https://github.com/agilbert1412/StardewArchipelago/blob/main/StardewArchipelago/Archipelago/StardewArchipelagoClient.cs
    public class GKArchipelagoClient : ArchipelagoClient
    {
        private const int ReconnectSucceeded = 1;
        private const int ReconnectRejected = 2;

        private readonly Harmony _harmony;
        private DeathManager _deathManager;
        private GameSave _checkSave;
        private SaveBoundLocationChecks _saveChecks;
        private LocationTriggerService _locationTriggers;
        private TechnologyGameChecks _technologyChecks;
        private int _reconnectResult;
        private readonly object _itemReceiverLock = new();
        private volatile ItemPacketReceiver _itemPackets;
        private ReceivedItemManager _receivedItems;
        private GrantContext _grantContext;
        private GoalManager _goalManager;
        private SlotData _goalSlot;
        internal PendingTrapRunner PendingTraps { get; private set; }
        
        public override string GameName => "Graveyard Keeper";
        public override string ModName => "GraveyardKeepelago";
        public override string ModVersion => PluginInfo.PLUGIN_VERSION;

        public SlotData SlotData => (SlotData)_slotData;
        internal QuestFlowChecks QuestFlowChecks { get; } = new QuestFlowChecks();
        internal ItemPacketReceiver ItemPackets => _itemPackets;
        internal ReceivedItemManager ReceivedItems => _receivedItems;
        internal GrantContext ItemGrantContext => _grantContext;

        public GKArchipelagoClient(ILogger logger, Harmony harmony, Action itemReceivedFunction) :
            base(logger,
                new GKDataPackageCache("graveyard_keeper", "BepInEx", "plugins", "GraveyardKeepelago", "IdTables"),
                itemReceivedFunction)
        {
            _harmony = harmony;
        }

        protected override void InitializeSlotData(string slotName, Dictionary<string, object> slotDataFields)
        {
            _slotData = new SlotData(slotName, slotDataFields, Logger);
        }

        public override ConnectionResult ConnectToMultiworld(ArchipelagoConnectionInfo connectionInfo)
        {
            ClearSaveChecks();
            var baseResult = base.ConnectToMultiworld(connectionInfo);
            if (!baseResult.Success)
                return baseResult;

            var versionRejection = SlotData.GetDataVersionRejection() ?? SlotData.GetGoalRejection();
            if (versionRejection != null)
            {
                DisconnectPermanently();
                return versionRejection;
            }

            if (!SlotData.DLCs.IsDLCStateCorrect(out var missingDLCs))
            {
                DisconnectPermanently();
                return new MissingDLCsConnectionResult(missingDLCs);
            }

            StartReceivingItems();
            return baseResult;
        }

        private void StartReceivingItems()
        {
            var slot = SlotData;
            if (!IsConnected || slot == null || !slot.HasDataVersion)
                return;

            var session = GetSession();
            if (session == null)
                return;

            ItemPacketReceiver receiver;
            lock (_itemReceiverLock)
            {
                if (!IsConnected || !session.Socket.Connected || SlotData != slot)
                    return;

                var previous = _itemPackets;
                receiver = new ItemPacketReceiver(session, slot);
                _itemPackets = receiver;
                previous?.Dispose();
            }

            try
            {
                receiver.RequestInitialHistory();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not request the initial AP item history: {ex.Message}");
            }
        }

        private void StopReceivingItems()
        {
            lock (_itemReceiverLock)
            {
                var receiver = _itemPackets;
                _itemPackets = null;
                receiver?.Dispose();
            }
        }

        internal LocationTriggerService GetLocationTriggers(GameSave save)
            => IsCurrentCheckSave(save) ? _locationTriggers : null;

        internal TechnologyGameChecks GetTechnologyChecks(GameSave save)
            => IsCurrentCheckSave(save) ? _technologyChecks : null;

        private bool IsCurrentCheckSave(GameSave save)
            => save != null && ReferenceEquals(_checkSave, save) && ReferenceEquals(MainGame.me?.save, save);

        internal void BindSaveChecks(GameSave save, bool newSave)
        {
            if (save == null || ReferenceEquals(_checkSave, save) || !IsConnected ||
                SlotData == null || SlotData.GetDataVersionRejection() != null || SlotData.GetGoalRejection() != null)
                return;

            ClearSaveChecks();
            if (!SlotData.HasDataVersion)
            {
                try
                {
                    var legacySession = GetSession();
                    if (legacySession == null || !legacySession.Socket.Connected || legacySession.ConnectionInfo.Game != GameName)
                        return;
                    _saveChecks = SaveBoundLocationChecks.Bind(save.unlocked_phrases, GetIdentity(legacySession),
                        new Dictionary<string, long>(), newSave);
                    _checkSave = save;
                    _goalManager = new GoalManager();
                    _goalSlot = SlotData;
                }
                catch (InvalidDataException ex)
                {
                    ClearSaveChecks();
                    Logger.LogError($"Archipelago progress could not be bound to this save: {ex.Message}");
                }
                return;
            }
            var connection = _itemPackets;
            if (!IsCurrentItemConnection(connection))
                return;

            var session = connection.Session;
            try
            {
                var active = session.Locations.AllLocations.ToDictionary(
                    id => session.Locations.GetLocationNameFromId(id, GameName), id => id);
                var contract = ClientContractLoader.LoadDefault();
                var catalog = TechnologyCatalog.LoadDefault();
                var registry = new LocationTriggerRegistry(contract, active.Keys, catalog);
                var saveChecks = SaveBoundLocationChecks.Bind(save.unlocked_phrases, GetIdentity(session), active, newSave);
                var triggers = new LocationTriggerService(saveChecks, registry);
                var enabledItems = contract.Items.Values.Where(item => connection.Slot.DLCs.IsContentEnabled(item.DLC)).ToArray();
                _technologyChecks = new TechnologyGameChecks(triggers, catalog, enabledItems,
                    GameBalance.me.techs_data.Select(tech => tech.flowscript).Where(script => !string.IsNullOrEmpty(script)));
                var grantFactory = new ItemGrantFactory(enabledItems, Logger);
                _receivedItems = new ReceivedItemManager(saveChecks.Progress, grantFactory.Create);
                _grantContext = new GrantContext(save, Logger);
                PendingTraps = new PendingTrapRunner(saveChecks.Progress, saveChecks.Progress.Store,
                    NativeMonsterTrap.Prepare, Logger.LogError, UnityEngine.Time.unscaledTime);
                _locationTriggers = triggers;
                _saveChecks = saveChecks;
                _checkSave = save;
                QuestFlowChecks.StartSlot(triggers, () => ReferenceEquals(MainGame.me?.save, save));
                SyncSaveChecks(session);
            }
            catch (InvalidDataException ex)
            {
                ClearSaveChecks();
                Logger.LogError($"Archipelago progress could not be bound to this save: {ex.Message}");
            }
        }

        internal void ClearSaveChecks()
        {
            QuestFlowChecks.Clear();
            _locationTriggers = null;
            _technologyChecks = null;
            _saveChecks?.Disconnect();
            _saveChecks = null;
            _checkSave = null;
            _receivedItems?.StopActiveGrant();
            _receivedItems = null;
            _grantContext = null;
            PendingTraps = null;
            _goalManager = null;
            _goalSlot = null;
            Interlocked.Exchange(ref _reconnectResult, 0);
        }

        internal void UpdateSaveChecks()
        {
            if (_saveChecks == null)
                return;

            var reconnectResult = Interlocked.Exchange(ref _reconnectResult, 0);
            if (!ReferenceEquals(_checkSave, MainGame.me?.save) || reconnectResult == ReconnectRejected)
            {
                ClearSaveChecks();
                return;
            }

            if (reconnectResult == ReconnectSucceeded && IsConnected)
            {
                if (SlotData.HasDataVersion) SyncSaveChecks(_itemPackets?.Session);
                else
                {
                    var session = GetSession();
                    if (session == null || _saveChecks.Progress.Identity != GetIdentity(session)) ClearSaveChecks();
                }
                if (_saveChecks != null)
                {
                    _goalManager = new GoalManager();
                    _goalSlot = SlotData;
                }
            }
        }

        internal void UpdateGoalCompletion(float now)
        {
            var session = GetSession();
            var save = _checkSave;
            var slot = SlotData;
            bool IsReady() => _goalManager != null && _saveChecks != null && IsCurrentCheckSave(save) &&
                IsConnected && session != null && ReferenceEquals(GetSession(), session) && session.Socket.Connected &&
                session.ConnectionInfo.Game == GameName && slot == SlotData && slot == _goalSlot &&
                slot.GetGoalRejection() == null && _saveChecks.Progress.Identity == GetIdentity(session);
            if (!IsReady()) return;
            _goalManager.Update(now, IsReady, () =>
            {
                float? rating = null;
                if (slot.Goal == Goal.Graveyard)
                {
                    var zone = WorldZone.GetZoneByID("graveyard", false);
                    if (zone != null) rating = zone.GetTotalQuality();
                }
                return GoalManager.IsComplete(slot.Goal, slot.GraveyardGoalRating, save.completed_one_time_crafts, rating);
            }, () => session.Socket.SendPacketAsync(new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal }),
                Logger.LogWarning);
        }

        private bool IsCurrentItemConnection(ItemPacketReceiver receiver)
        {
            if (receiver == null || receiver != _itemPackets || !IsConnected)
                return false;

            if (!receiver.Session.Socket.Connected || receiver.Slot != SlotData)
                return false;

            return receiver.Session.ConnectionInfo.Game == GameName;
        }

        internal bool IsCurrentItemSave(ItemPacketReceiver receiver, ReceivedItemManager manager)
        {
            if (manager != _receivedItems || !IsCurrentItemConnection(receiver))
                return false;

            return IsCurrentCheckSave(_checkSave) && _saveChecks.Progress.Identity == GetIdentity(receiver.Session);
        }
        private void SyncSaveChecks(IArchipelagoSession session)
        {
            if (session == null)
                return;

            var activeIDs = new HashSet<long>(session.Locations.AllLocations);
            if (SlotData?.HasDataVersion != true ||
                !_saveChecks.Connect(GetIdentity(session), ids => _ = SendSaveChecks(session, activeIDs, ids)))
            {
                ClearSaveChecks();
                Logger.LogError("Archipelago check recording stopped because the connected slot changed.");
            }
            else
            {
                _goalManager = new GoalManager();
                _goalSlot = SlotData;
            }
        }

        private async Task SendSaveChecks(IArchipelagoSession session, HashSet<long> activeIDs, long[] ids)
        {
            // Use the validated session directly. The legacy report method can reconnect.
            if (!session.Socket.Connected)
                return;

            var active = ids.Where(activeIDs.Contains).ToArray();
            if (active.Length == 0)
                return;

            try
            {
                await session.Locations.CompleteLocationChecksAsync(active).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Archipelago check transmission failed. Saved checks can be resent on reconnect: {ex.Message}");
            }
        }

        private static (string Seed, int Team, int Slot) GetIdentity(IArchipelagoSession session)
            => (session.RoomState.Seed, session.ConnectionInfo.Team, session.ConnectionInfo.Slot);

        protected override void InitializeDeathLink()
        {
            _deathManager = new DeathManager(this.Logger, _harmony, this);
            _deathManager.HookIntoDeathLinkEvents();
            base.InitializeDeathLink();
        }

        protected override void OnMessageReceived(LogMessage message)
        {
            var fullMessage = string.Join(" ", message.Parts.Select(str => str.Text));
            this.Logger.LogInfo($"Message received: {fullMessage}");

            switch (message)
            {
                case AdminCommandResultLogMessage:
                case ChatLogMessage chatMessage:
                    return;
                case CollectLogMessage:
                case CommandResultLogMessage:
                case CountdownLogMessage:
                case GoalLogMessage:
                    return;
                case HintItemSendLogMessage:
                    return;
                case ItemCheatLogMessage:
                case ItemSendLogMessage itemSendLogMessage:
                    return;
                case JoinLogMessage:
                case LeaveLogMessage:
                    return;
                case ReleaseLogMessage:
                    return;
                case ServerChatLogMessage:
                    return;
                case TagsChangedLogMessage:
                case TutorialLogMessage:
                    return;
            }
        }

        protected override void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            this.Logger.LogInfo("Packet received: " + packet.ToString());
        }

        protected override void KillPlayerDeathLink(DeathLink deathlink)
        {
            this.Logger.LogInfo("DeathLink received: " + deathlink.ToString());
            DeathManager.ReceiveDeathLink();
        }

        public override void DisconnectAndCleanup()
        {
            StopReceivingItems();
            base.DisconnectAndCleanup();
            _deathManager = null;
        }

        protected override void OnError(string reason, Exception e = null)
        {
            Logger.LogError(e == null ? $"OnErrorReceived: {reason}" : $"OnErrorReceived: {e?.Message} {reason}");
            base.OnError(reason, e);
        }

        protected override void OnReconnectFailure()
        {
            base.OnReconnectFailure();
        }

        protected override void OnReconnectSuccess()
        {
            var rejection = SlotData?.GetDataVersionRejection() ?? SlotData?.GetGoalRejection();
            if (rejection != null)
            {
                Logger.LogError(rejection.Message);
                DisconnectPermanently();
                Interlocked.Exchange(ref _reconnectResult, ReconnectRejected);
                return;
            }

            StartReceivingItems();
            // Game-save reads and replay run on the Unity thread.
            Interlocked.Exchange(ref _reconnectResult, ReconnectSucceeded);
            base.OnReconnectSuccess();
        }

        /*
        private ScoutingPreference ShouldHint(bool createAsHint)
        {
            if (!createAsHint)
                return ScoutingPreference.DontHint;

            var session = GetSession();
            if (!MakeSureConnected() || session == null)
                return ScoutingPreference.DontHint;

            // TODO: depend on the player's/world's settings
            return ScoutingPreference.HintEverything;
        }

        private ScoutedLocation ScoutGKLocation(string locationName, bool createAsHint = false)
        {
            var scoutBehavior = ShouldHint(createAsHint);
            ScoutedLocation scoutedLocation;
            if (scoutBehavior == ScoutingPreference.HintEverything)
                scoutedLocation = ScoutSingleLocation(locationName, true);
            else
            {
                scoutedLocation = ScoutSingleLocation(locationName, false);
                if (scoutedLocation == null)
                    return null;
                var shouldHint = ShouldHintAfterScouting(scoutBehavior, scoutedLocation);
                if (shouldHint)
                    ScoutSingleLocation(locationName, true);
            }

            return scoutedLocation;
        }
        */

        public GKDataPackageCache DataPackageCache => (GKDataPackageCache)LocalDataPackage;
    }
}
