using System.Reflection;
using System.Reflection.Emit;
using GraveyardKeepelago.GameModifications;
using HarmonyLib;

namespace GraveyardKeepelago.Tests;

public class SleepAccessTests
{
    [Theory]
    [InlineData(100f, 100f)]
    [InlineData(100f, 50f)]
    [InlineData(50f, 100f)]
    [InlineData(50f, 50f)]
    public void RewrittenEntryGuardAllowsSleepWithoutAnyApSession(float energy, float sanity)
    {
        var original = EntryCheck();
        Assert.Equal(energy == 100f && sanity == 100f, Compile(original)(energy, sanity));
        var patched = SleepEntryInstructions.AllowFullEnergySleep(original).ToArray();
        Assert.False(Compile(patched)(energy, sanity));
        Assert.Equal(2, patched.Count(instruction => instruction.opcode == OpCodes.Call));
        Assert.Equal(original.Length + 2, patched.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void UnexpectedNativeLayoutFailsExplicitly(int comparisons)
    {
        var call = EntryCheck().First(instruction => instruction.opcode == OpCodes.Call);
        Assert.Throws<InvalidOperationException>(() =>
            SleepEntryInstructions.AllowFullEnergySleep(Enumerable.Repeat(call, comparisons)).ToArray());
    }

    private static bool EqualsTo(float actual, float expected, float tolerance) => Math.Abs(actual - expected) < tolerance;

    private static CodeInstruction[] EntryCheck()
    {
        var comparison = typeof(SleepAccessTests).GetMethod(nameof(EqualsTo), BindingFlags.Static | BindingFlags.NonPublic)!;
        return new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldc_R4, 100f),
            new CodeInstruction(OpCodes.Ldc_R4, 0.00001f), new CodeInstruction(OpCodes.Call, comparison),
            new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Ldc_R4, 100f),
            new CodeInstruction(OpCodes.Ldc_R4, 0.00001f), new CodeInstruction(OpCodes.Call, comparison),
            new CodeInstruction(OpCodes.And), new CodeInstruction(OpCodes.Ret),
        };
    }

    private static Func<float, float, bool> Compile(IEnumerable<CodeInstruction> instructions)
    {
        var method = new DynamicMethod("SleepEntry", typeof(bool), new[] { typeof(float), typeof(float) },
            typeof(SleepAccessTests), true);
        var generator = method.GetILGenerator();
        foreach (var instruction in instructions)
            if (instruction.operand is MethodInfo comparison) generator.Emit(instruction.opcode, comparison);
            else if (instruction.operand is float value) generator.Emit(instruction.opcode, value);
            else generator.Emit(instruction.opcode);
        return (Func<float, float, bool>)method.CreateDelegate(typeof(Func<float, float, bool>));
    }
}
