using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.Patches;

[HarmonyPatch(typeof(PlayerBuff), nameof(PlayerBuff.CustomUpdate))]
internal static class PermanentBuffUpdatePatch
{
    // Shared AP grants mark the individual saved buff. Vanilla instances of
    // the same definition retain their duration and tick behavior.
    private static bool Prefix(PlayerBuff __instance) => !IsPermanent(__instance.end_time);

    internal static bool IsPermanent(float endTime) => endTime == float.MaxValue;
}
