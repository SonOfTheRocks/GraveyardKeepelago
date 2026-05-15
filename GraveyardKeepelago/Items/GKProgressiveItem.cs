using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items;

public abstract class GKProgressiveItem: IAPItem
{
    public List<List<string>> progressiveIDs;
    public int applyCount { get; set; }
    protected IPlayerActions PlayerActions { get; }

    public GKProgressiveItem(List<List<string>> ids, IPlayerActions playerActions = null)
    {
        progressiveIDs = ids;
        applyCount = 0;
        PlayerActions = playerActions;
    }

    public abstract void Apply();
}