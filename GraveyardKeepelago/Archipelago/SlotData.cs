using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        public const string GRAVEYARD_GOAL_RATING_KEY = "graveyard_goal_rating";
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
        public int? GraveyardGoalRating { get; private set; }
        public bool DeathLink { get; private set; }
        public string Seed { get; private set; }
        public string MultiworldVersion { get; private set; }
        public int DataVersion { get; private set; }
        public DLCManager DLCs { get; set; }

        public SlotData(string slotName, Dictionary<string, object> slotDataFields, ILogger logger)
        {
            SlotName = slotName;
            _slotDataFields = slotDataFields;
            _logger = logger;

            //DeathLink = GetSlotSetting(DEATH_LINK_KEY, false);
            //Seed = GetSlotSetting(SEED_KEY, "0");
            MultiworldVersion = GetSlotSetting(MULTIWORLD_VERSION_KEY, PluginInfo.PLUGIN_VERSION);
            _logger.LogInfo($"GraveyardKeepelago version '{PluginInfo.PLUGIN_VERSION}' connected to a multiworld generated with APWorld version '{MultiworldVersion}'.");
            DataVersion = GetSlotSetting(DATA_VERSION_KEY, 0);

            Goal = ReadGoal();
            if (Goal == Goal.Graveyard)
                GraveyardGoalRating = ReadGraveyardGoalRating();
            DLCs = new DLCManager(_logger, GetDLCList());
        }

        private Goal ReadGoal()
        {
            // Older portal slots may omit the setting; a supplied invalid value
            // must never silently change the generated completion condition.
            if (!_slotDataFields.TryGetValue(GOAL_KEY, out var value)) return Goal.Portal;
            if ((value is int || value is long || value is string) &&
                Enum.TryParse<Goal>(value.ToString(), true, out var goal) && Enum.IsDefined(typeof(Goal), goal))
                return goal;
            throw new InvalidDataException("Slot data goal is unsupported. Update the client or regenerate the seed with a supported goal.");
        }

        private int ReadGraveyardGoalRating()
        {
            if (_slotDataFields.TryGetValue(GRAVEYARD_GOAL_RATING_KEY, out var value))
            {
                if (value is int rating && rating >= 0 && rating <= 2484) return rating;
                if (value is long longRating && longRating >= 0 && longRating <= 2484) return (int)longRating;
            }
            throw new InvalidDataException("Slot data graveyard_goal_rating must be an integer from 0 to 2484. " +
                "Regenerate this Graveyard-goal seed with an APWorld that supplies its required rating.");
        }

        private List<string> GetDLCList()
        {
            return DLCKeys.Where(key => GetSlotSetting(key, true)).ToList();
        }

        private T GetSlotSetting<T>(string key, T defaultValue) where T : struct, Enum
        {
            if (_slotDataFields.ContainsKey(key) && _slotDataFields[key] != null)
                if (Enum.TryParse<T>(_slotDataFields[key].ToString(), true, out var parsedValue))
                    return parsedValue;
            
            return GetSlotDefaultValue(key, defaultValue);
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

        private int GetSlotSetting(string key, int defaultValue)
        {
            if (_slotDataFields.ContainsKey(key) && _slotDataFields[key] != null)
            {
                switch (_slotDataFields[key])
                {
                    case int intValue:
                        return intValue;
                    case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                        return (int)longValue;
                    default:
                        if (int.TryParse(_slotDataFields[key].ToString(), out var parsedValue))
                            return parsedValue;
                        break;
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
