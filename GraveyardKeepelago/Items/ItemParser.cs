using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items
{
    public class ItemParser
    {
        public static readonly string BLUEPRINT_PREFIX = "Blueprint: ";
        public static readonly  string BUFF_PREFIX = "Buff: ";
        public static readonly  string EXTRACT_PREFIX = "Extract: ";
        public static readonly  string GATHERING_PREFIX = "Gathering: ";
        public static readonly  string RECIPE_PREFIX = "Create: ";
        public static readonly  string PERK_PREFIX = "Perk: ";
        public static readonly  string RELATION_INFIX = " - Happiness";
        //public const string UNLOCK_PREFIX = "Unlock: ";
        private readonly List<string> RECIPE_PREFIXES = new List<string> {BLUEPRINT_PREFIX, EXTRACT_PREFIX, GATHERING_PREFIX, RECIPE_PREFIX};
        
        private readonly ILogger _logger;
        private readonly GKItemManager _itemManager;
        private readonly TrapManager _trapManager;

        public ItemParser(
            ILogger logger,
            Harmony harmony,
            GKArchipelagoClient archipelago,
            LocationChecker locationChecker,
            GKItemManager itemManager,
            TrapExecutor trapExecutor
        )
        {
            _logger = logger;
            _itemManager = itemManager;
            _trapManager = new TrapManager(logger, harmony, archipelago, trapExecutor);
        }
        
        public TrapManager TrapManager => _trapManager;

        public void ProcessItem(ReceivedItem receivedItem)
        {
            var apItemName = receivedItem.ItemName;
            _logger.LogInfo($"Received Item '{apItemName}'");
            if (_trapManager.IsTrap(receivedItem.ItemName))
            {
                _trapManager.TryExecuteTrapImmediately(receivedItem.ItemName);
                return;
            }
         
            IAPItem item;
            if (apItemName.StartsWith(BUFF_PREFIX))
            {
                item = _itemManager.GetBuffItem(apItemName);
            }
            else if (RECIPE_PREFIXES.Any(p => apItemName.StartsWith(p)))
            {
                item = _itemManager.GetRecipeItem(apItemName);
            }
            else if (apItemName.StartsWith(PERK_PREFIX))
            {
                item = _itemManager.GetPerkItem(apItemName);
            }
            else if (apItemName.Contains(RELATION_INFIX))
            {
                var match = Regex.Match(apItemName, $@"^(.*){RELATION_INFIX}\s\+(\d+)$");
                if (!match.Success)
                {
                    _logger.LogError($"Could not process relation item {receivedItem.ItemName}");
                    return;
                }

                var npc = match.Groups[1].Value;
                var amount = int.Parse(match.Groups[2].Value);
                item = _itemManager.GetRelationItem(npc, amount);
            }
            else if (_itemManager.TryGetIngameItem(apItemName, out var ingameItem))
            {
                item = ingameItem;
            }
            else
            {
                _logger.LogError($"Could not process item {receivedItem.ItemName}");
                return;
            }
            
            item.Apply();
        }
    }
}