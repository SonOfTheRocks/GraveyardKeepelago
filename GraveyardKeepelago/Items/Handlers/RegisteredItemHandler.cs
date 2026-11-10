using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Handlers;

public class RegisteredItemHandler : IItemHandler
{
    public int Priority => 100;

    public bool Matches(string itemName) => true;

    public IAPItem Create(string itemName, GKItemRegistry registry)
    {
        return registry.TryGet(itemName, out var item) ? item : null;
    }
}
