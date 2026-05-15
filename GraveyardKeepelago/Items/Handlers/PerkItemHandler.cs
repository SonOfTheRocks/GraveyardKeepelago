using System;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Handlers;

public class PerkItemHandler : IItemHandler
{
    public const string Prefix = "Perk: ";
    private readonly GKItemRegistry _registry;

    public int Priority => 30;

    public PerkItemHandler(GKItemRegistry registry)
    {
        _registry = registry;
    }

    public bool Matches(string itemName) => itemName.StartsWith(Prefix);

    public IAPItem Create(string itemName, GKItemRegistry registry)
    {
        return _registry.GetPerk(itemName);
    }
}
