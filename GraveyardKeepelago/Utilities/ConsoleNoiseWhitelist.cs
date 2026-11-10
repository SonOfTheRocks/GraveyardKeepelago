using System;
using System.Text.RegularExpressions;
using BepInEx.Logging;

namespace GraveyardKeepelago.Utilities;

public static class ConsoleNoiseWhitelist
{
    public const string NativeInventoryLayerDiagnostic = "[AP UI diagnostics] Widget 'UI Root/Inventory GUI/Multi inventory panel/scroll panel/content table/separator' on layer 0 belongs to panel 'UI Root/Inventory GUI/Multi inventory panel/scroll panel' on layer 13.";
    public const string NativeLayerRepairWarning = "You can't place widgets on a layer different than the UIPanel that manages them.\nIf you want to move widgets to a different layer, parent them to a new panel instead.";
    private static readonly Regex DuplicateSprite = new Regex(
        @"\AError adding sprite to a library - duplicate name: [^\r\n]+\z", RegexOptions.CultureInvariant);
    private static readonly Regex MissingAsset = new Regex(
        "\\AError loading asset \"[^\"\\r\\n]+\"\\z", RegexOptions.CultureInvariant);
    private static readonly Regex MissingZone = new Regex(
        @"\A(?:Could't|Couldn't) find zone \[[^\]\r\n]+\]\z", RegexOptions.CultureInvariant);

    public static bool Suppress(string source, LogLevel level, object message)
    {
        // Even when the user's console configuration is All, diagnostic detail
        // belongs in the disk log. Never hide a warning/error combined with it.
        if ((level & (LogLevel.Fatal | LogLevel.Error | LogLevel.Warning)) == 0)
        {
            if ((level & (LogLevel.Debug | LogLevel.Info | LogLevel.Message)) == LogLevel.Debug)
                return true;
            // Unity Debug.Log is reported as Info, including frame-by-frame GUI
            // redraws and resource paths. AP messages use our own log source.
            if (string.Equals(source, "Unity Log", StringComparison.Ordinal) &&
                (level & (LogLevel.Info | LogLevel.Message | LogLevel.Debug)) != 0)
                return true;
        }
        // Match the observed source, severity and complete payload, so other
        // graph/world-object failures and additional context stay visible.
        if (!string.Equals(source, "Unity Log", StringComparison.Ordinal) || !(message is string text))
            return false;
        return level == LogLevel.Warning && (
                   string.Equals(text, NativeInventoryLayerDiagnostic, StringComparison.Ordinal) ||
                   text.StartsWith(NativeInventoryLayerDiagnostic + "\n", StringComparison.Ordinal) ||
                   string.Equals(text, NativeLayerRepairWarning, StringComparison.Ordinal) ||
                   string.Equals(text, "Cannot use 'MovePosition' on a static body.", StringComparison.Ordinal) ||
                   string.Equals(text, "<b>(NodeCanvas Warning)</b>: Graph is already Active.", StringComparison.Ordinal) ||
                   DuplicateSprite.IsMatch(text) || MissingAsset.IsMatch(text)) ||
               level == LogLevel.Error && (
                   string.Equals(text, "World Object not found", StringComparison.Ordinal) ||
                   string.Equals(text, "Can't deserialize WGO with an empty id.", StringComparison.Ordinal) ||
                   MissingZone.IsMatch(text));
    }
}
