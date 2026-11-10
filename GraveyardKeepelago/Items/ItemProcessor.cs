using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;
using System;
using GraveyardKeepelago.Items.Handlers;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Locations;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items;

public class ItemProcessor : IItemProcessor
{
    private readonly ILogger _logger;
    private readonly List<IItemHandler> _handlers;
    private readonly GKItemRegistry _registry;
    public Action<ReceivedItem, IAPItem> ItemApplied { get; set; }

    public ITrapManager TrapManager => _trapManager;
    private readonly ITrapManager _trapManager;

    public ItemProcessor(ILogger logger, GKItemRegistry registry, ITrapManager trapManager)
    {
        _logger = logger;
        _registry = registry;
        _trapManager = trapManager;
        
        _handlers = new List<IItemHandler>
        {
            new TrapItemHandler(logger, trapManager, registry),
            new RegisteredItemHandler(),
        };
        
        _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void ProcessItem(ReceivedItem receivedItem)
        => TryProcessItem(receivedItem);

    public bool TryProcessItem(ReceivedItem receivedItem)
    {
        using (LocationTriggerRuntime.EnterArchipelagoItemApplication())
            return ProcessItemInternal(receivedItem);
    }

    private bool ProcessItemInternal(ReceivedItem receivedItem)
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
                // Trap handlers return null after queuing successfully.
                if (handler is not TrapItemHandler)
                    _logger.LogError($"Could not process item {itemName}");
                return handler is TrapItemHandler;
            }
            
            item.Apply();
            // Presentation failures must not replay an already granted reward.
            try { ItemApplied?.Invoke(receivedItem, item); }
            catch (Exception ex) { _logger.LogError($"Could not queue notification for {itemName}: {ex}"); }
            return true;
        }
        
        _logger.LogError($"Could not process item {itemName}");
        return false;
    }
}
