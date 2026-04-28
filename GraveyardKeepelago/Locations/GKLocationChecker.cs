using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Archipelago;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Locations
{
    public class GKLocationChecker : LocationChecker, ILocationChecker
    {
        private readonly GKArchipelagoClient _archipelago;
        private readonly LocationNameMatcher _locationNameMatcher;

        public GKLocationChecker(ILogger logger, GKArchipelagoClient archipelago/*, List<string> locationsAlreadyChecked*/)
            : base(logger, archipelago/*, locationsAlreadyChecked*/, new List<string>())
        {
            _archipelago = archipelago;
            _locationNameMatcher = new LocationNameMatcher();
        }

        public override void SendAllLocationChecks()
        {
            base.SendAllLocationChecks();
        }

        public override void ClearCache()
        {
            base.ClearCache();
            _locationNameMatcher.ClearCache();
        }

        public IEnumerable<string> GetAllLocationsNotChecked(string filter)
        {
            return _locationNameMatcher.GetAllLocationsMatching(GetAllLocationsNotChecked(), filter);
        }

        public IEnumerable<string> GetAllLocationsNotCheckedMatchingExactly(string filter)
        {
            return _locationNameMatcher.GetAllLocationsMatchingExactly(GetAllLocationsNotChecked(), filter);
        }

        public IEnumerable<string> GetAllLocationsNotCheckedStartingWith(string prefix)
        {
            return _locationNameMatcher.GetAllLocationsStartingWith(GetAllLocationsNotChecked(), prefix);
        }

        public IEnumerable<string> GetAllLocationsStartingWith(string prefix)
        {
            return _locationNameMatcher.GetAllLocationsStartingWith(GetAllLocations(), prefix);
        }

        public IEnumerable<string> GetAllLocationsNotCheckedContainingWord(string wordFilter)
        {
            return _locationNameMatcher.GetAllLocationsContainingWord(GetAllLocationsNotChecked(), wordFilter);
        }

        public bool IsAnyLocationNotChecked(string filter)
        {
            return _locationNameMatcher.IsAnyLocationMatching(GetAllLocationsNotChecked(), filter);
        }

        public bool IsAnyLocationNotCheckedStartingWith(string prefix)
        {
            return _locationNameMatcher.IsAnyLocationStartingWith(GetAllLocationsNotChecked(), prefix);
        }

        public bool IsAnyLocationNotCheckedEndingWith(string suffix)
        {
            return _locationNameMatcher.IsAnyLocationEndingWith(GetAllLocationsNotChecked(), suffix);
        }

        public IEnumerable<string> GetLocationsByTag(string locationTag)
        {
            return GetAllLocations().Where(x => _archipelago.DataPackageCache.GetLocation(x).LocationTags.Contains(locationTag));
        }

        public IEnumerable<string> GetMissingLocationsByTag(string locationTag)
        {
            return GetAllMissingLocationNames().Where(x => _archipelago.DataPackageCache.GetLocation(x).LocationTags.Contains(locationTag));
        }

        public IEnumerable<string> GetCheckedLocationsByTag(string locationTag)
        {
            return GetAllLocationsAlreadyChecked().Where(x => _archipelago.DataPackageCache.GetLocation(x).LocationTags.Contains(locationTag));
        }

        public double GetPercentLocationsChecked()
        {
            var numChecked = GetAllLocationsAlreadyChecked().Count;
            var numTotal = GetAllLocations().Count();
            return (double)numChecked / (double)numTotal;
        }
    }
}