using System;
using System.Collections.Generic;
using System.Linq;
using KaitoKid.ArchipelagoUtilities.Net.Interfaces;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Archipelago
{
    public class SlotData : ISlotData
    {   
        private const string DEATH_LINK_KEY = "death_link";
        private const string SEED_KEY = "seed";
        private const string MULTIWORLD_VERSION_KEY = "client_version";

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
        public bool DeathLink { get; private set; }
        public string Seed { get; private set; }
        public string MultiworldVersion { get; private set; }
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

            Goal = GetSlotSetting(GOAL_KEY, Goal.Portal);
            DLCs = new DLCManager(_logger, GetDLCList());
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

        private T GetSlotDefaultValue<T>(string key, T defaultValue)
        {
            _logger.LogWarning($"SlotData did not contain expected key: \"{key}\"");
            return defaultValue;
        }
    }
}