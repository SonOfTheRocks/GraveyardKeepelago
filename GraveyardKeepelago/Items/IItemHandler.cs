using System.Collections.Generic;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items;

public interface IItemHandler
{
    int Priority { get; }
    bool Matches(string itemName);
    IAPItem Create(string itemName, GKItemRegistry registry);
}
