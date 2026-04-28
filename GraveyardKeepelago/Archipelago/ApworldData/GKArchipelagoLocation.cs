using System.Collections.Generic;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Archipelago.ApworldData
{
    public class GKArchipelagoLocation: ArchipelagoLocation
    {
        public HashSet<string> LocationTags { get; private set; }

        public GKArchipelagoLocation(string name, long id, HashSet<string> locationTags) : base(name, id)
        {
            LocationTags = new HashSet<string>(locationTags);
        }
    }
}