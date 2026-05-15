using System;
using GraveyardKeepelago.Items.Traps;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Handlers;

public class TrapItemHandler : IItemHandler
{
    private readonly ILogger _logger;
    private readonly ITrapManager _trapManager;

    public int Priority => 0; // Highest priority - checked first

    public TrapItemHandler(ILogger logger, ITrapManager trapManager)
    {
        _logger = logger;
        _trapManager = trapManager;
    }

    public bool Matches(string itemName) => _trapManager.IsTrap(itemName);

    public Items.IAPItem Create(string itemName, Logic.GKItemRegistry registry)
    {
        _trapManager.TryExecuteTrapImmediately(itemName);
        return null; // Traps don't produce items
    }
}
