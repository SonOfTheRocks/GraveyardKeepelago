using System.Reflection;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.Deathlink.Patches
{
    //public void WasHitBy(CombatComponent other, ObjectDefinition.DamageType damage_type)
    [HarmonyPatch (typeof(CombatComponent), nameof(CombatComponent.WasHitBy))]
    public class CombatComponentWasHitByPatch : BasePatch
    {
        public static string lastToAttackPlayer { get; private set; }

        public static bool Prefix(CombatComponent __instance, CombatComponent other, ObjectDefinition.DamageType damage_type, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod, new LogParam[]
            {
                new("other", other),
                new("damage_type", damage_type),
            });

            var dockedObj = Traverse.Create(__instance.components.character).Field("_docked_obj").GetValue<WorldGameObject>();
            var isPlayer = Traverse.Create(dockedObj).Field("_is_player").GetValue<bool>();
            if (isPlayer)
            {
                Logger.LogInfo($"Player got attacked by: {other.wgo.name}");
                // TODO: get localization of attacker
                lastToAttackPlayer = other.wgo.name;
            }

            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        public static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}