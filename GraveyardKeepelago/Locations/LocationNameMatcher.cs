using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Locations
{
    public class LocationNameMatcher
    {
        private readonly Dictionary<string, string[]> _wordFilterCache;

        public LocationNameMatcher()
        {
            _wordFilterCache = new Dictionary<string, string[]>();
        }
        
        public IEnumerable<string> GetAllLocationsMatchingExactly(IEnumerable<string> allLocations, string filter)
        {
            return allLocations.Where(x => x.Equals(filter, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<string> GetAllLocationsMatching(IEnumerable<string> allLocations, string filter)
        {
            return allLocations.Where(x => x.Equals(filter, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<string> GetAllLocationsStartingWith(IEnumerable<string> allLocations, string prefix)
        {
            return allLocations.Where(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<string> GetAllLocationsEndingWith(IEnumerable<string> allLocations, string suffix)
        {
            return allLocations.Where(x => x.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase));
        }

        public string[] GetAllLocationsContainingWord(IEnumerable<string> allLocations, string wordFilter)
        {
            if (_wordFilterCache.ContainsKey(wordFilter))
                return _wordFilterCache[wordFilter];
            
            var filteredLocations = FilterForWord(GetAllLocationsMatching(allLocations, wordFilter), wordFilter).ToArray();

            _wordFilterCache.Add(wordFilter, filteredLocations);
            return filteredLocations;
        }

        public bool IsAnyLocationMatching(IEnumerable<string> allLocations, string filter)
        {
            return GetAllLocationsMatching(allLocations, filter).Any();
        }

        public bool IsAnyLocationStartingWith(IEnumerable<string> allLocations, string prefix)
        {
            return GetAllLocationsStartingWith(allLocations, prefix).Any();
        }

        public bool IsAnyLocationEndingWith(IEnumerable<string> allLocations, string suffix)
        {
            return GetAllLocationsEndingWith(allLocations, suffix).Any();
        }

        private static IEnumerable<string> FilterForWord(IEnumerable<string> allLocations, string filterWord)
        {
            return allLocations.Where(location => ItemIsRelevant(filterWord, location));
        }

        private static bool ItemIsRelevant(string itemName, string locationName)
        {
            var index = locationName.IndexOf(itemName, StringComparison.InvariantCultureIgnoreCase);
            if (index < 0)
                return false;

            var atStart = index == 0;
            var atEnd = index + itemName.Length >= locationName.Length;

            var validStart = atStart || locationName[index - 1] == ' ';
            var validEnd = atEnd || locationName[index + itemName.Length] == ' ';

            return validStart && validEnd;
        }
        
        public void ClearCache()
        {
            _wordFilterCache.Clear();
        }
    }
}