using GraveyardKeepelago.Technologies;
using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.Patches;

[HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetParam))]
public static class MorgueAccessPatch
{
    private static bool Prefix(WorldGameObject __instance, string param_name, ref float __result)
    {
        var apActive = TechnologyRuntime.Current?.IsActive == true;
        var isPlayer = ReferenceEquals(__instance, MainGame.me?.player);
        // Gerry sets lock_tp + reason 1 before granting First slice. Teleport
        // checks this general lock before reaching either morgue-specific flag.
        // Read the raw item data to avoid recursively calling this patched method.
        var lockReason = apActive && isPlayer && param_name == "lock_tp"
            ? __instance.data.GetParam("lock_tp_param") : 0f;
        if (!MorgueAccessPolicy.ShouldBypassFlag(apActive, isPlayer, param_name, lockReason) &&
            !TutorialAccessPolicy.IgnoreTutorialStatus(apActive, isPlayer, param_name))
            return true;

        // Override the read so existing saves are repaired too, and a later tutorial
        // write cannot reintroduce the lock. Leave the saved tutorial state intact.
        __result = 0;
        return false;
    }
}
