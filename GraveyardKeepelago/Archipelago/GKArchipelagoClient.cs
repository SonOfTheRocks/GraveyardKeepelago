using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using GraveyardKeepelago.Archipelago.ConnectionResults;
using GraveyardKeepelago.Deathlink;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Archipelago
{
    // Most of this class is based on the Stardew Archipelago Mod: https://github.com/agilbert1412/StardewArchipelago/blob/main/StardewArchipelago/Archipelago/StardewArchipelagoClient.cs
    public class GKArchipelagoClient : ArchipelagoClient
    {
        private readonly Harmony _harmony;
        private DeathManager _deathManager;
        
        public override string GameName => "Graveyard Keeper";
        public override string ModName => "GraveyardKeepelago";
        public override string ModVersion => PluginInfo.PLUGIN_VERSION;

        public SlotData SlotData => (SlotData)_slotData;

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
            var baseResult = base.ConnectToMultiworld(connectionInfo);
            if (!baseResult.Success)
                return baseResult;

            if (!SlotData.DLCs.IsDLCStateCorrect(out var missingDLCs))
            {
                DisconnectPermanently();
                return new MissingDLCsConnectionResult(missingDLCs);
            }

            return baseResult;
        }

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