using System;
using GraveyardKeepelago.Items.Traps;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Handlers;

public class TrapItemHandler : IItemHandler
{
    private readonly ILogger _logger;
    private readonly ITrapManager _trapManager;
    private readonly Logic.GKItemRegistry _registry;

    public int Priority => 0; // Highest priority - checked first

    public TrapItemHandler(ILogger logger, ITrapManager trapManager, Logic.GKItemRegistry registry = null)
    {
        _logger = logger;
        _trapManager = trapManager;
        _registry = registry;
    }

    public bool Matches(string itemName) => !(_registry?.TryGet(itemName, out _) ?? false) && _trapManager.IsTrap(itemName);

    public Items.IAPItem Create(string itemName, Logic.GKItemRegistry registry)
    {
        if (!_trapManager.TryExecuteTrapImmediately(itemName))
            throw new InvalidOperationException($"Trap '{itemName}' could not be queued.");
        return null; // Traps don't produce items
    }
}
