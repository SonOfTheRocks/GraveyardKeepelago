using System;
using System.Linq;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Handlers;

public class RecipeItemHandler : IItemHandler
{
    public const string BlueprintPrefix = "Blueprint: ";
    public const string ExtractPrefix = "Extract: ";
    public const string GatheringPrefix = "Gathering: ";
    public const string CreatePrefix = "Create: ";
    
    private static readonly string[] Prefixes = { BlueprintPrefix, ExtractPrefix, GatheringPrefix, CreatePrefix };
    
    private readonly GKItemRegistry _registry;

    public int Priority => 20;

    public RecipeItemHandler(GKItemRegistry registry)
    {
        _registry = registry;
    }

    public bool Matches(string itemName) => Prefixes.Any(p => itemName.StartsWith(p));

    public IAPItem Create(string itemName, GKItemRegistry registry)
    {
        return _registry.GetRecipe(itemName);
    }
}
