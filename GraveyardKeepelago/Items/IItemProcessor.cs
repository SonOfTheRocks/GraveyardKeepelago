using System.Collections.Generic;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Items;

public interface IItemProcessor
{
    void ProcessItem(ReceivedItem receivedItem);
}
