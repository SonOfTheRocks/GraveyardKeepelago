using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Handlers;

public class IngameItemHandler : IItemHandler
{
    private readonly GKItemRegistry _registry;

    public int Priority => 100; // Lowest priority - fallback

    public IngameItemHandler(GKItemRegistry registry)
    {
        _registry = registry;
    }

    public bool Matches(string itemName) => true; // Always matches as fallback

    public IAPItem Create(string itemName, GKItemRegistry registry)
    {
        return _registry.TryGetIngameItem(itemName, out var item) ? item : null;
    }
}
