using System.Collections.Generic;
using System.Linq;
using System;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Archipelago;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Locations
{
    public class GKLocationChecker : LocationChecker, ILocationChecker
    {
        private readonly GKArchipelagoClient _archipelago;
        private readonly LocationNameMatcher _locationNameMatcher;
        private readonly SaveProgress _progress;
        private readonly Action _store;
        private readonly Dictionary<string, long> _activeLocations;

        public GKLocationChecker(ILogger logger, GKArchipelagoClient archipelago, SaveProgress progress = null, Action store = null)
            : base(logger, archipelago, progress?.Checks.ToList() ?? new List<string>())
        {
            _archipelago = archipelago;
            _locationNameMatcher = new LocationNameMatcher();
            _progress = progress;
            _store = store;
            _activeLocations = archipelago.GetAllLocations();
            if (progress != null)
                _checkedLocations = progress.Checks.Where(_activeLocations.ContainsKey)
                    .ToDictionary(name => name, name => _activeLocations[name]);
        }

        public override void AddCheckedLocation(string locationName) => AddCheckedLocations(new[] { locationName });

        public override void AddCheckedLocations(string[] locationNames)
        {
            var enabled = locationNames.Where(_activeLocations.ContainsKey).Distinct().ToArray();
            if (_progress == null) { base.AddCheckedLocations(enabled); return; }
            var added = _progress.RememberChecks(enabled, _activeLocations);
            foreach (var name in added) _checkedLocations[name] = _activeLocations[name];
            if (added.Length > 0)
            {
                ClearCache();
                _store?.Invoke();
                if (_archipelago.IsConnected)
                {
                    foreach (var name in added) _logger.LogInfo($"Sending AP check: {name}");
                    _archipelago.ReportCheckedLocations(added.Select(name => _activeLocations[name]).ToArray());
                }
                else
                    foreach (var name in added) _logger.LogInfo($"AP check saved offline: {name}");
            }
        }

        public override void SendAllLocationChecks()
        {
            if (_progress == null) { base.SendAllLocationChecks(); return; }
            if (_archipelago.IsConnected)
            {
                var checks = _progress.Checks.Where(_activeLocations.ContainsKey)
                    .Select(name => _activeLocations[name]).ToArray();
                if (checks.Length == 0) return;
                _logger.LogInfo($"Synchronizing {checks.Length} saved AP checks.");
                _archipelago.ReportCheckedLocations(checks);
            }
        }

        public override void ClearCache()
        {
            base.ClearCache();
            _locationNameMatcher?.ClearCache();
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
