using System.Collections.Generic;
using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.Patches;

[HarmonyPatch(typeof(SleepGUI), nameof(SleepGUI.Open))]
public static class FullEnergySleepPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        SleepEntryInstructions.AllowFullEnergySleep(instructions);
}
