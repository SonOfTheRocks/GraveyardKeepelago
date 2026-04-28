using System.Reflection;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.Deathlink.Patches
{
    //public void OnPlayerDied()
    [HarmonyPatch(typeof(MainGame), nameof(MainGame.OnPlayerDied))]
    public class MainGameOnPlayerDiedPatch : BasePatch
    {
        public static bool Prefix(MethodBase __originalMethod)
        {
            LogBefore(__originalMethod);

            DeathManager.SendDeathLink($"was killed by {CombatComponentWasHitByPatch.lastToAttackPlayer}");
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        public static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}