using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace GraveyardKeepelago.GameModifications;

public static class SleepEntryInstructions
{
    public static IEnumerable<CodeInstruction> AllowFullEnergySleep(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var comparisons = code.Select((instruction, index) => new { instruction, index })
            .Where(entry => entry.instruction.operand is MethodInfo method && method.Name == "EqualsTo" &&
                method.ReturnType == typeof(bool) &&
                method.GetParameters().Select(parameter => parameter.ParameterType)
                    .SequenceEqual(new[] { typeof(float), typeof(float), typeof(float) }) &&
                (entry.instruction.opcode == OpCodes.Call || entry.instruction.opcode == OpCodes.Callvirt))
            .Select(entry => entry.index).ToArray();
        if (comparisons.Length != 2)
            throw new InvalidOperationException("SleepGUI.Open no longer has the expected energy/sanity entry guard.");
        // Keep the native call; replace only its result so the original branch
        // skips the refusal for every save, without changing player stats.
        code.InsertRange(comparisons[0] + 1, new[]
        {
            new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_I4_0),
        });
        return code;
    }
}
