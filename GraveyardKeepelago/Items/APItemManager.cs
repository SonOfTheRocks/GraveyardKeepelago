using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items
{
    public class APItemManager: ItemManager
    {
        private readonly ItemProcessor _itemProcessor;
        
        public ItemProcessor ItemProcessor => _itemProcessor;
        public ITrapManager TrapManager => _itemProcessor.TrapManager;

        public APItemManager(
            ILogger logger,
            Harmony harmony,
            GKArchipelagoClient archipelago,
            LocationChecker locationChecker,
            GKItemRegistry registry,
            TrapExecutor trapExecutor/*,
            IEnumerable<ReceivedItem> itemsAlreadyProcessed*/) : base(archipelago/*, itemsAlreadyProcessed*/, new List<ReceivedItem>())
        {
            _itemProcessor = new ItemProcessor(logger, harmony, archipelago, registry, trapExecutor);
        }

        protected override void ProcessItem(ReceivedItem receivedItem, bool immediatelyIfPossible)
        {
            _itemProcessor.ProcessItem(receivedItem);
        }
    }
}
