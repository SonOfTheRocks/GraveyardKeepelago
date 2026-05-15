using System.Text.RegularExpressions;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Handlers;

public class RelationItemHandler : IItemHandler
{
    public const string Infix = " - Happiness";
    private readonly GKItemRegistry _registry;

    public int Priority => 40;

    public RelationItemHandler(GKItemRegistry registry)
    {
        _registry = registry;
    }

    public bool Matches(string itemName) => itemName.Contains(Infix);

    public IAPItem Create(string itemName, GKItemRegistry registry)
    {
        var match = Regex.Match(itemName, $@"^(.*){Infix}\s\+(\d+)$");
        if (!match.Success) return null;
        
        var npc = match.Groups[1].Value;
        return _registry.GetRelation(npc);
    }
}
