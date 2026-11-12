using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using GraveyardKeepelago.Archipelago.ConnectionResults;
using GraveyardKeepelago.Deathlink;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.Utilities.Interfaces;

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
        private TechnologyGameChecks _technologyChecks;
        private int _reconnectResult;
        
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

            return baseResult;
        }

        internal TechnologyGameChecks GetTechnologyChecks(GameSave save)
            => ReferenceEquals(_checkSave, save) && ReferenceEquals(MainGame.me?.save, save) ? _technologyChecks : null;

        internal void BindSaveChecks(GameSave save, bool newSave)
        {
            if (save == null || ReferenceEquals(_checkSave, save) || !IsConnected ||
                SlotData?.HasDataVersion != true || SlotData.GetDataVersionRejection() != null)
                return;

            ClearSaveChecks();
            var session = GetSession();
            if (session == null)
                return;

            try
            {
                var active = session.Locations.AllLocations.ToDictionary(
                    id => session.Locations.GetLocationNameFromId(id, GameName), id => id);
                var contract = ClientContractLoader.LoadDefault();
                var catalog = TechnologyCatalog.LoadDefault();
                var registry = new LocationTriggerRegistry(contract, active.Keys, catalog);
                var saveChecks = SaveBoundLocationChecks.Bind(save.unlocked_phrases, GetIdentity(session), active, newSave);
                var triggers = new LocationTriggerService(saveChecks, registry);
                _technologyChecks = new TechnologyGameChecks(triggers, catalog,
                    contract.Items.Values.Where(item => SlotData.DLCs.IsContentEnabled(item.DLC)),
                    GameBalance.me.techs_data.Select(tech => tech.flowscript).Where(script => !string.IsNullOrEmpty(script)));
                _saveChecks = saveChecks;
                _checkSave = save;
                QuestFlowChecks.StartSlot(triggers, () => ReferenceEquals(MainGame.me?.save, save));
                SyncSaveChecks(session);
            }
            catch (InvalidDataException ex)
            {
                Logger.LogError($"Archipelago checks could not be bound to this save: {ex.Message}");
            }
        }

        internal void ClearSaveChecks()
        {
            QuestFlowChecks.Clear();
            _technologyChecks = null;
            _saveChecks?.Disconnect();
            _saveChecks = null;
            _checkSave = null;
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
                SyncSaveChecks(GetSession());
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
