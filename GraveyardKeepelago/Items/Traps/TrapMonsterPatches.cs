using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace GraveyardKeepelago.Items.Traps;

[HarmonyPatch]
internal static class TrapMonsterUpdatePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(WorldGameObject), nameof(WorldGameObject.CustomUpdate));
        yield return AccessTools.Method(typeof(WorldGameObject), nameof(WorldGameObject.CustomFixedUpdate));
        yield return AccessTools.Method(typeof(WorldGameObject), nameof(WorldGameObject.CustomLateUpdate));
    }
    private static bool Prefix(WorldGameObject __instance) => TrapMonsterRuntime.Current?.CanUpdate(__instance) ?? true;
}

[HarmonyPatch(typeof(CombatComponent), nameof(CombatComponent.WasHitBy))]
internal static class TrapMonsterCombatPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(CombatComponent __instance, CombatComponent other)
        => TrapMonsterRuntime.Current?.CanHit(other?.wgo, __instance.wgo) ?? true;
}

[HarmonyPatch(typeof(MainGame), nameof(MainGame.OnPlayerDied))]
internal static class TrapMonsterDeathPatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix() => TrapMonsterRuntime.Current?.Clear();
}

[HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.TeleportToGDPoint))]
internal static class TrapMonsterTeleportPatch
{
    private static void Prefix(WorldGameObject __instance)
    { if (__instance.is_player) TrapMonsterRuntime.Current?.Clear(); }
}

[HarmonyPatch(typeof(SerializableGameMap), nameof(SerializableGameMap.SaveSceneToMe))]
internal static class TrapMonsterSavePatch
{
    private static void Postfix(List<SerializableWGO> ____wgos)
    {
        var runtime = TrapMonsterRuntime.Current;
        if (runtime != null) ____wgos.RemoveAll(monster => !runtime.CanSerialize(monster.unique_id));
    }
}

[HarmonyPatch(typeof(WorldZone), nameof(WorldZone.OnPlayerExit))]
internal static class TrapMonsterZoneExitPatch
{
    private static void Prefix() => TrapMonsterRuntime.Current?.Clear();
}
