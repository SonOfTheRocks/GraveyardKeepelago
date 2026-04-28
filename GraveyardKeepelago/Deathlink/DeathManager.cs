using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Deathlink.Patches;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Deathlink
{
    public class DeathManager
    {
        private static ILogger _logger;
        private static Harmony _harmony;
        private static GKArchipelagoClient _archipelago;
        
        private static bool _isCurrentlyReceivingDeathLink = false;

        public DeathManager(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago)
        {
            _logger = logger;
            _harmony = harmony;
            _archipelago = archipelago;
        }

        public static void ReceiveDeathLink()
        {
            if (!_archipelago.DeathLink)
                return;
            
            _isCurrentlyReceivingDeathLink = true;
            // TODO: Kill Player
            _logger.LogWarning("TODO: Kill player");
            _isCurrentlyReceivingDeathLink = false;
        }

        public static void SendDeathLink(string cause)
        {
            if (!_archipelago.DeathLink)
                return;

            if (_isCurrentlyReceivingDeathLink)
                return;
            
            _archipelago.SendDeathLink(cause);
        }

        public void HookIntoDeathLinkEvents()
        {
            HookIntoDeathEvent();
            HookIntoHitEvent();
        }

        private void HookIntoDeathEvent()
        {
            _harmony.CreateClassProcessor(typeof(MainGameOnPlayerDiedPatch)).Patch();
        }

        private void HookIntoHitEvent()
        {
            _harmony.CreateClassProcessor(typeof(CombatComponentWasHitByPatch)).Patch();
        }
    }
}