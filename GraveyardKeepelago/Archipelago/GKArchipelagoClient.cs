using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Archipelago.ConnectionResults;
using GraveyardKeepelago.Deathlink;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.Utilities.Interfaces;
using Time = UnityEngine.Time;
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
        private volatile ItemSession _itemSession;
        private ReceivedItemManager _receivedItems;
        private float _nextDeliveryAt;
        private string _deliveryError;
        
        public override string GameName => "Graveyard Keeper";
        public override string ModName => "GraveyardKeepelago";
        public override string ModVersion => PluginInfo.PLUGIN_VERSION;

        public SlotData SlotData => (SlotData)_slotData;
        internal QuestFlowChecks QuestFlowChecks { get; } = new QuestFlowChecks();

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

            var versionRejection = SlotData.GetDataVersionRejection();
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

            AcceptItemSession();
            return baseResult;
        }

        private void AcceptItemSession()
        {
            var slot = SlotData;
            var session = IsConnected && slot?.HasDataVersion == true ? GetSession() : null;
            if (session == null)
                return;

            var items = new ItemSession(session, slot);
            session.Socket.PacketReceived += items.Receive;

            var previous = Interlocked.Exchange(ref _itemSession, items);
            previous?.Unsubscribe();
            if (!IsConnected || !session.Socket.Connected)
            {
                Interlocked.CompareExchange(ref _itemSession, null, items);
                items.Unsubscribe();
            }
        }

        private void ReleaseItemSession()
        {
            Interlocked.Exchange(ref _itemSession, null)?.Unsubscribe();
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
                SlotData?.HasDataVersion != true || SlotData.GetDataVersionRejection() != null)
                return;

            ClearSaveChecks();
            var connection = _itemSession;
            if (connection?.Slot.HasDataVersion != true || SlotData != connection.Slot ||
                !connection.Session.Socket.Connected || connection.Session.ConnectionInfo.Game != GameName)
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
            _nextDeliveryAt = 0;
            _deliveryError = null;
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
                SyncSaveChecks(_itemSession?.Session);
        }

        internal void UpdateItemDelivery()
        {
            var connection = _itemSession;
            var items = _receivedItems;
            var save = _checkSave;
            if (connection?.Slot.HasDataVersion != true || items == null || !CanDeliver(connection, save) ||
                Time.unscaledTime < _nextDeliveryAt)
                return;

            // Also polls after a silent initial batch and retries an unconfirmed head.
            _nextDeliveryAt = Time.unscaledTime + 1f;
            try
            {
                var history = connection.History.Snapshot(out var syncGeneration);
                if (history == null)
                {
                    if (connection.RequestedSync != syncGeneration)
                    {
                        // An empty slot does not send a response to Sync.
                        connection.Session.Socket.SendPacket(new SyncPacket());
                        connection.RequestedSync = syncGeneration;
                        var checkedLocations = connection.Session.Locations.AllLocationsChecked.ToArray();
                        if (checkedLocations.Length > 0)
                            _ = SendSaveChecks(connection.Session,
                                new HashSet<long>(connection.Session.Locations.AllLocations), checkedLocations);
                    }
                    return;
                }

                items.ProcessReceivedItems(history, () => CanDeliver(connection, save) &&
                    (items.HasPendingGrant || (MainGame.me.player_char != null && GS.IsPlayerEnable())) &&
                    ReferenceEquals(history, connection.History.Snapshot(out _)),
                    id => connection.Session.Items.GetItemName(id, GameName)
                        ?? throw new InvalidDataException($"No Graveyard Keeper Data Package name for item ID {id}."),
                    (grant, queueTrap) => ApplyGrant(save, grant, queueTrap));
                _deliveryError = null;
            }
            catch (Exception ex)
            {
                if (_deliveryError != ex.Message)
                    Logger.LogError($"Archipelago item delivery paused: {ex}");
                _deliveryError = ex.Message;
            }
        }

        private GrantResult ApplyGrant(GameSave save, IItemGrant grant, Action<string> queueTrap)
        {
            var context = new GrantContext(save, trapID =>
            {
                queueTrap(trapID);
                return true;
            }, Logger);
            return grant.Apply(context);
        }

        private bool CanDeliver(ItemSession connection, GameSave save)
        {
            var player = MainGame.me?.player;
            var gui = GUIElements.me;
            return ReferenceEquals(_itemSession, connection) && IsConnected && connection.Session.Socket.Connected &&
                SlotData == connection.Slot && connection.Session.ConnectionInfo.Game == GameName &&
                _saveChecks?.Progress.Identity == GetIdentity(connection.Session) && IsCurrentCheckSave(save) &&
                MainGame.game_started && !MainGame.paused && player != null && !player.is_dead && player.hp > 0 &&
                gui != null && !GUIElements.gui_is_initializing && !LoadingGUI.is_shown &&
                gui.main_menu?.is_shown != true;
        }

        private sealed class ItemSession
        {
            public ArchipelagoSession Session { get; }
            public SlotData Slot { get; }
            public ReceivedItemHistory History { get; } = new();
            public int RequestedSync { get; set; } = -1;

            public ItemSession(ArchipelagoSession session, SlotData slot)
            {
                Session = session;
                Slot = slot;
            }

            public void Unsubscribe() => Session.Socket.PacketReceived -= Receive;

            public void Receive(ArchipelagoPacketBase packet)
            {
                if (packet is ReceivedItemsPacket items)
                    History.Receive(items.Index, items.Items);
            }
        }

        private void SyncSaveChecks(ArchipelagoSession session)
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
        }

        private async Task SendSaveChecks(ArchipelagoSession session, HashSet<long> activeIDs, long[] ids)
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

        private static (string Seed, int Team, int Slot) GetIdentity(ArchipelagoSession session)
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
            ReleaseItemSession();
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
            var rejection = SlotData?.GetDataVersionRejection();
            if (rejection != null)
            {
                Logger.LogError(rejection.Message);
                DisconnectPermanently();
                Interlocked.Exchange(ref _reconnectResult, ReconnectRejected);
                return;
            }

            AcceptItemSession();
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
