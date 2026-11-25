using System;
using System.Collections.Generic;
using System.Linq;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.ArchipelagoUtilities.Net.Interfaces;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Archipelago
{
    public class SlotData : ISlotData
    {   
        private const string DEATH_LINK_KEY = "death_link";
        private const string SEED_KEY = "seed";
        private const string MULTIWORLD_VERSION_KEY = "client_version";
        public const string DATA_VERSION_KEY = "graveyard_keeper_data_version";

        private const string GOAL_KEY = "goal";
        public const string DLC_STRANGER_SINS_KEY = "stranger_sins";
        public const string DLC_GAME_OF_CRONE_KEY = "game_of_crone";
        public const string DLC_BETTER_SAVE_SOUL_KEY = "better_save_soul";

        private static readonly List<string> DLCKeys = new()
        {
            DLC_STRANGER_SINS_KEY,
            DLC_GAME_OF_CRONE_KEY,
            DLC_BETTER_SAVE_SOUL_KEY,
        };
        
        private Dictionary<string, object> _slotDataFields;
        private ILogger _logger;
        
        public string SlotName { get; private set; }
        public Goal Goal { get; private set; }
        public int GraveyardGoalRating { get; private set; }
        internal string GoalValidationError { get; private set; }
        public bool DeathLink { get; private set; }
        public string Seed { get; private set; }
        public string MultiworldVersion { get; private set; }
        public DLCManager DLCs { get; set; }
        internal bool HasDataVersion => _slotDataFields.ContainsKey(DATA_VERSION_KEY);

        public SlotData(string slotName, Dictionary<string, object> slotDataFields, ILogger logger)
        {
            SlotName = slotName;
            _slotDataFields = slotDataFields;
            _logger = logger;

            //DeathLink = GetSlotSetting(DEATH_LINK_KEY, false);
            //Seed = GetSlotSetting(SEED_KEY, "0");
            MultiworldVersion = GetSlotSetting(MULTIWORLD_VERSION_KEY, PluginInfo.PLUGIN_VERSION);
            _logger.LogInfo($"GraveyardKeepelago version '{PluginInfo.PLUGIN_VERSION}' connected to a multiworld generated with APWorld version '{MultiworldVersion}'.");

            if (!TryInteger(GOAL_KEY, 0, 1, out var goal))
                GoalValidationError = "Slot data requires an integer goal: 0 (Portal) or 1 (Graveyard).";
            else
            {
                Goal = (Goal)goal;
                if (Goal == Goal.Graveyard)
                {
                    if (!TryInteger("graveyard_goal_rating", 0, 2484, out var rating))
                        GoalValidationError = "Graveyard slots require an integer graveyard_goal_rating from 0 through 2484.";
                    else
                        GraveyardGoalRating = rating;
                }
            }
            DLCs = new DLCManager(_logger, GetDLCList());
        }

        public FailedConnectionResult GetDataVersionRejection()
        {
            // Embedding a manifest does not enable its gameplay contract.
            if (!_slotDataFields.TryGetValue(DATA_VERSION_KEY, out var version))
                return null;

            return new FailedConnectionResult(
                $"This client cannot play slots with Graveyard Keeper data version {version ?? "(missing value)"}. " +
                "Use a client that supports the slot's data version.");
        }

        internal FailedConnectionResult GetGoalRejection() => GoalValidationError == null
            ? null : new FailedConnectionResult(GoalValidationError);

        private bool TryInteger(string key, int minimum, int maximum, out int result)
        {
            result = 0;
            if (!_slotDataFields.TryGetValue(key, out var value)) return false;
            long number;
            switch (value)
            {
                case int integer: number = integer; break;
                case long integer: number = integer; break;
                default: return false;
            }
            if (number < minimum || number > maximum) return false;
            result = (int)number;
            return true;
        }

        private List<string> GetDLCList()
        {
            return DLCKeys.Where(key => GetSlotSetting(key, true)).ToList();
        }

        private string GetSlotSetting(string key, string defaultValue)
        {
            return _slotDataFields.ContainsKey(key) ? _slotDataFields[key].ToString() : GetSlotDefaultValue(key, defaultValue);
        }

        private bool GetSlotSetting(string key, bool defaultValue)
        {
            if (_slotDataFields.ContainsKey(key) && _slotDataFields[key] != null)
            {
                switch (_slotDataFields[key])
                {
                    case bool boolValue:
                        return boolValue;
                    case long longValue:
                        return longValue != 0;
                    case int intValue:
                        return intValue != 0;
                }
            }
            
            return GetSlotDefaultValue(key, defaultValue);
        }

        private T GetSlotDefaultValue<T>(string key, T defaultValue)
        {
            _logger.LogWarning($"SlotData did not contain expected key: \"{key}\"");
            return defaultValue;
        }
    }
}
