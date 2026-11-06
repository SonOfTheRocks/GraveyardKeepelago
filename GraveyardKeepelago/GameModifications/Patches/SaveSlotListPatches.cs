using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GraveyardKeepelago.Progression;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.Patches;

internal static class SaveSlotListState
{
    private static readonly ConditionalWeakTable<SaveSlotGUI, State> Rows = new();
    internal static bool Blocked(SaveSlotGUI row) => Rows.TryGetValue(row, out var state) && state.Reason != null;
    internal static void Show(SaveSlotGUI row, SaveSlotData save)
    {
        var state = Rows.GetValue(row, item => new State(item));
        state.Tooltip.Hide();
        state.Tooltip.ClearData();
        state.Reason = SaveSlotEligibility.Reason(Plugin.Instance?.ConnectedSaveGuard(), save == null, () => Read(save));
        for (var index = 0; index < state.Widgets.Length; index++)
            if (state.Widgets[index] != null)
                state.Widgets[index].color = state.Reason == null ? state.Colors[index] :
                    state.Colors[index] * new Color(0.45f, 0.45f, 0.45f, 1);
        if (state.Reason != null) state.Tooltip.SetText(state.Reason);
    }

    private static System.Collections.Generic.IList<string> Read(SaveSlotData slot)
    {
        var name = slot.filename_no_extension;
        if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name))
            throw new IOException("The save filename is invalid.");
        var path = Path.Combine(PlatformSpecific.GetSaveFolder(), name + ".dat");
        // Deserialize only. Do not use PlatformSpecific.LoadGame: it changes the
        // selected slot and enters the world's loading continuation.
        var clock = Stopwatch.StartNew();
        var binary = slot.IsBinaryFormat();
        var bytes = binary ? File.ReadAllBytes(path) : null;
        var json = binary ? null : File.ReadAllText(path);
        var ioMs = clock.Elapsed.TotalMilliseconds;
        System.Collections.Generic.IList<string> phrases;
        var mode = binary ? "phrase projection" : "json";
        if (binary)
        {
            try { phrases = SavePhraseReader.Read(bytes); }
            catch (NotSupportedException)
            {
                mode = "native fallback";
                var full = GameSave.FromBinary(bytes);
                if (full == null) throw new IOException("The save could not be read.");
                phrases = full.unlocked_phrases;
            }
        }
        else
        {
            var full = GameSave.FromJSON(json);
            if (full == null) throw new IOException("The save could not be read.");
            phrases = full.unlocked_phrases;
        }
        var deserializeMs = clock.Elapsed.TotalMilliseconds - ioMs;
        // Console filter keeps Unity info in the disk log. Do not log save contents.
        UnityEngine.Debug.Log($"[AP startup timing] save-slot inspection: io={ioMs:F1}ms, " +
            $"deserialize={deserializeMs:F1}ms, total={clock.Elapsed.TotalMilliseconds:F1}ms, " +
            $"format={(binary ? "binary" : "json")}, mode={mode}");
        return phrases;
    }

    internal static void Hover(SaveSlotGUI row)
    {
        if (!Rows.TryGetValue(row, out var state) || state.Reason == null) return;
        state.Tooltip.Show(BaseGUI.for_gamepad);
        if (BaseGUI.for_gamepad)
        {
            var menu = Traverse.Create(row).Field("_menu").GetValue<SaveSlotsMenuGUI>();
            menu?.button_tips.Print(GameKeyTip.Select(false), GameKeyTip.Option2("delete"), GameKeyTip.Close());
        }
    }

    internal static void Out(SaveSlotGUI row)
    {
        if (Rows.TryGetValue(row, out var state)) state.Tooltip.Hide();
    }

    private sealed class State
    {
        internal string Reason;
        internal readonly Tooltip Tooltip;
        internal readonly UIWidget[] Widgets;
        internal readonly Color[] Colors;
        internal State(SaveSlotGUI row)
        {
            Tooltip = row.gameObject.AddComponent<Tooltip>();
            Tooltip.Init();
            Widgets = new UIWidget[] { row.back, row.slot_name, row.txt_realtime, row.txt_descr, row.txt_stats };
            Colors = Array.ConvertAll(Widgets, widget => widget == null ? Color.white : widget.color);
        }
    }
}

[HarmonyPatch(typeof(SaveSlotGUI), nameof(SaveSlotGUI.Show))]
public static class SaveSlotListShowPatch
{
    private static void Postfix(SaveSlotGUI __instance, SaveSlotData save) => SaveSlotListState.Show(__instance, save);
}

[HarmonyPatch(typeof(SaveSlotGUI), nameof(SaveSlotGUI.OnSlotSelect))]
public static class SaveSlotListSelectPatch
{
    private static bool Prefix(SaveSlotGUI __instance) => !SaveSlotListState.Blocked(__instance);
}

[HarmonyPatch(typeof(SaveSlotGUI), nameof(SaveSlotGUI.OnOver))]
public static class SaveSlotListHoverPatch
{
    private static void Postfix(SaveSlotGUI __instance) => SaveSlotListState.Hover(__instance);
}

[HarmonyPatch(typeof(SaveSlotGUI), nameof(SaveSlotGUI.OnOut))]
public static class SaveSlotListOutPatch
{
    private static void Prefix(SaveSlotGUI __instance) => SaveSlotListState.Out(__instance);
}
