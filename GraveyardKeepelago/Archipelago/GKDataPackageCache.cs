using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Archipelago.ApworldData;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Interfaces;

namespace GraveyardKeepelago.Archipelago
{
    public class GKDataPackageCache: DataPackageCache
    {
        public GKDataPackageCache(string snakeCaseGameName, params string[] pathsToFolder):
            base(snakeCaseGameName, pathsToFolder)
        {}
        
        public GKDataPackageCache(IJsonLoader jsonLoader, string snakeCaseGameName, params string[] pathsToFolder):
            base(jsonLoader, snakeCaseGameName, pathsToFolder)
        {}
        
        public GKDataPackageCache(IArchipelagoLoader<ArchipelagoItem> itemLoader, IArchipelagoLoader<ArchipelagoLocation> locationLoader, string snakeCaseGameName, params string[] pathsToFolder):
            base(itemLoader, locationLoader, snakeCaseGameName, pathsToFolder)
        {}

        public IEnumerable<GKArchipelagoLocation> GetAllLocations()
        {
            return _locationCacheById.Values.Cast<GKArchipelagoLocation>();
        }

        public GKArchipelagoLocation GetLocation(string locationName)
        {
            return (GKArchipelagoLocation)_locationCacheByName[locationName];
        }

        public IEnumerable<ArchipelagoItem> GetAllItems()
        {
            return _itemCacheById.Values.Cast<ArchipelagoItem>();
        }
    }
}