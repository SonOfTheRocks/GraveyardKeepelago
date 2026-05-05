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
        private readonly ItemParser _itemParser;
        
        public ItemParser ItemParser => _itemParser;
        public ITrapManager TrapManager => _itemParser.TrapManager;

        public APItemManager(
            ILogger logger,
            Harmony harmony,
            GKArchipelagoClient archipelago,
            LocationChecker locationChecker,
            GKItemManager itemManager,
            TrapExecutor trapExecutor/*,
            IEnumerable<ReceivedItem> itemsAlreadyProcessed*/) : base(archipelago/*, itemsAlreadyProcessed*/, new List<ReceivedItem>())
        {
            _itemParser = new ItemParser(logger, harmony, archipelago, locationChecker, itemManager, trapExecutor);
        }

        protected override void ProcessItem(ReceivedItem receivedItem, bool immediatelyIfPossible)
        {
            _itemParser.ProcessItem(receivedItem);
        }
    }
}
