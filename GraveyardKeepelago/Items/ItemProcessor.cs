using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items.Handlers;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items;

public class ItemProcessor : IItemProcessor
{
    private readonly ILogger _logger;
    private readonly List<IItemHandler> _handlers;
    private readonly GKItemRegistry _registry;

    public ITrapManager TrapManager => _trapManager;
    private readonly ITrapManager _trapManager;

    public ItemProcessor(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago, GKItemRegistry registry, TrapExecutor trapExecutor)
        : this(logger, registry, new TrapManager(logger, harmony, archipelago, trapExecutor))
    {
    }

    public ItemProcessor(ILogger logger, GKItemRegistry registry, ITrapManager trapManager)
    {
        _logger = logger;
        _registry = registry;
        _trapManager = trapManager;
        
        _handlers = new List<IItemHandler>
        {
            new TrapItemHandler(logger, trapManager),
            new BuffItemHandler(registry),
            new RecipeItemHandler(registry),
            new PerkItemHandler(registry),
            new RelationItemHandler(registry),
            new IngameItemHandler(registry),
        };
        
        _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void ProcessItem(ReceivedItem receivedItem)
    {
        var itemName = receivedItem.ItemName;
        _logger.LogInfo($"Received Item '{itemName}'");
        
        foreach (var handler in _handlers)
        {
            if (!handler.Matches(itemName))
                continue;
            
            var item = handler.Create(itemName, _registry);
            if (item == null)
            {
                // Trap handlers return null after executing - that's fine
                if (handler is not TrapItemHandler)
                    _logger.LogError($"Could not process item {itemName}");
                return;
            }
            
            item.Apply();
            return;
        }
        
        _logger.LogError($"Could not process item {itemName}");
    }
}
