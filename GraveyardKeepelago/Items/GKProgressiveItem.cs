using System.Collections.Generic;

namespace GraveyardKeepelago.Items;

public abstract class GKProgressiveItem: IAPItem
{
    public List<List<string>> progressiveIDs;
    public int applyCount { get; set; }

    public GKProgressiveItem(List<List<string>> ids)
    {
        progressiveIDs = ids;
        applyCount = 0;
    }

    public abstract void Apply();
}