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
            base(new EmptyLoader<ArchipelagoItem>(), new EmptyLoader<ArchipelagoLocation>(), snakeCaseGameName, pathsToFolder)
        {}
        
        public GKDataPackageCache(IJsonLoader jsonLoader, string snakeCaseGameName, params string[] pathsToFolder):
            base(jsonLoader, snakeCaseGameName, pathsToFolder)
        {}
        
        public GKDataPackageCache(IArchipelagoLoader<ArchipelagoItem> itemLoader, IArchipelagoLoader<ArchipelagoLocation> locationLoader, string snakeCaseGameName, params string[] pathsToFolder):
            base(itemLoader, locationLoader, snakeCaseGameName, pathsToFolder)
        {}

        // The former shipped ID tables were empty. Server Data Packages remain authoritative;
        // initialize the same empty fallback without requiring installation-time JSON files.
        private sealed class EmptyLoader<T> : IArchipelagoLoader<T>
        {
            public IEnumerable<T> LoadAll(params string[] paths) => Enumerable.Empty<T>();
            public T Load(string name, Newtonsoft.Json.Linq.JToken data) =>
                throw new System.NotSupportedException("The local fallback contains no item or location definitions.");
        }
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

