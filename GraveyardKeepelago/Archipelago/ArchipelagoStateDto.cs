using System.Collections.Generic;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Archipelago
{
    public class ArchipelagoStateDto
    {
        public ArchipelagoConnectionInfo APConnectionInfo { get; set; }
        public List<ReceivedItem> ItemsReceived { get; set; }
        public List<string> LocationsChecked { get; set; }

        public ArchipelagoStateDto()
        {
            ItemsReceived = new List<ReceivedItem>();
            LocationsChecked = new List<string>();
        }
    }
}