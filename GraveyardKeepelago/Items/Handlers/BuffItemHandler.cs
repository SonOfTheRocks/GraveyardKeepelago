namespace GraveyardKeepelago.Items.Handlers;

public class BuffItemHandler : IItemHandler
{
    public const string Prefix = "Permanent Buff: ";
    private readonly Logic.GKItemRegistry _registry;

    public int Priority => 10;

    public BuffItemHandler(Logic.GKItemRegistry registry)
    {
        _registry = registry;
    }

    public bool Matches(string itemName) => itemName.StartsWith(Prefix);

    public IAPItem Create(string itemName, Logic.GKItemRegistry registry)
    {
        return _registry.GetBuff(itemName);
    }
}
