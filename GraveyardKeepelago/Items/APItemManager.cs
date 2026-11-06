using System;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.Utilities.Interfaces;
using GraveyardKeepelago.Items.Notifications;

namespace GraveyardKeepelago.Items
{
    public class APItemManager
    {
        private readonly ItemProcessor _itemProcessor;
        private readonly GKArchipelagoClient _archipelago;
        private readonly ItemReceiptLedger _ledger;
        private readonly TrapManager _traps;
        
        public ItemProcessor ItemProcessor => _itemProcessor;
        public ITrapManager TrapManager => _itemProcessor.TrapManager;

        public APItemManager(
            ILogger logger,
            Harmony harmony,
            GKArchipelagoClient archipelago,
            LocationChecker locationChecker,
            GKItemRegistry registry,
            TrapExecutor trapExecutor, SaveProgress progress, Action store)
        {
            _archipelago = archipelago;
            _ledger = new ItemReceiptLedger(progress, store);
            _traps = new TrapManager(logger, progress, store, trapExecutor.SpawnMonster,
                () => ItemNotificationWindow.CanShow() && MainGame.me.player != null &&
                      !MainGame.me.player.is_dead && MainGame.me.player.hp > 0,
                () => UnityEngine.Time.unscaledTime);
            _itemProcessor = new ItemProcessor(logger, registry, _traps);
            _ledger.RestoreProgressiveStages(registry);
        }

        public void QueueTrap(string trapID) => _traps.Enqueue(trapID);
        public void UpdateTraps() => _traps.DequeueTrap();

        public void ReceiveAllNewItems()
        {
            if (_archipelago.IsConnected)
                _ledger.ApplyHistory(_archipelago.GetAllReceivedItems(), _itemProcessor.TryProcessItem);
        }
    }
}
