using BepInEx.Logging;
using GraveyardKeepelago.Utilities;
using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.Patches;

// Filter this listener only. DiskLogListener continues to receive the full event.
[HarmonyPatch(typeof(ConsoleLogListener), nameof(ConsoleLogListener.LogEvent))]
public static class ConsoleNoisePatch
{
    private static bool Prefix(LogEventArgs eventArgs) => eventArgs == null ||
        !ConsoleNoiseWhitelist.Suppress(eventArgs.Source?.SourceName, eventArgs.Level, eventArgs.Data);
}
