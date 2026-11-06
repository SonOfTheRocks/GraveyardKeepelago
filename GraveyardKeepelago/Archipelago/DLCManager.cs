using System.Collections.Generic;
using System.Linq;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Archipelago
{
    public class DLCManager
    {
        private readonly ILogger _logger;
        private readonly List<string> _activeDLCs;

        public DLCManager(ILogger logger, List<string> activeDLCs)
        {
            _logger = logger;
            _activeDLCs = activeDLCs;
        }

        public bool IsDLCStateCorrect(out List<string> missingDLCs)
        {
            missingDLCs = new List<string>();
            foreach (var activeDLC in _activeDLCs)
                if (dlcMapping.TryGetValue(activeDLC, out var dlcVersion))
                {
                    if (!DLCEngine.IsDLCAvailable(dlcVersion))
                        missingDLCs.Add(activeDLC);
                }
                else
                    _logger.LogError($"DLC option '{activeDLC}' is unknown");

            return !missingDLCs.Any();
        }

        public bool IsContentEnabled(string contentDLC) => contentDLC switch
        {
            "BASE_GAME" => true,
            "STRANGER_SINS" => _activeDLCs.Contains(SlotData.DLC_STRANGER_SINS_KEY),
            "GAME_OF_CRONE" => _activeDLCs.Contains(SlotData.DLC_GAME_OF_CRONE_KEY),
            "BETTER_SAVE_SOUL" => _activeDLCs.Contains(SlotData.DLC_BETTER_SAVE_SOUL_KEY),
            _ => throw new System.ArgumentException($"Unknown shared DLC '{contentDLC}'"),
        };

        private Dictionary<string, DLCEngine.DLCVersion> dlcMapping = new ()
        {
            { SlotData.DLC_STRANGER_SINS_KEY, DLCEngine.DLCVersion.Stories },
            { SlotData.DLC_GAME_OF_CRONE_KEY, DLCEngine.DLCVersion.Refugees },
            { SlotData.DLC_BETTER_SAVE_SOUL_KEY, DLCEngine.DLCVersion.Souls },
        };
    }
}
