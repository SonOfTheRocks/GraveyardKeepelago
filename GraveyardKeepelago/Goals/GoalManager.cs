using System;
using System.Linq;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;
using UnityEngine;

namespace GraveyardKeepelago.Goals;

public sealed class GoalManager : IDisposable
{
    private static GoalManager _current;
    private readonly ILogger _logger;
    private readonly Harmony _harmony;
    private readonly GKArchipelagoClient _archipelago;
    private readonly GameSave _save;
    private readonly GoalCompletionTracker _completion;
    private float _nextCheckAt, _nextFailureLogAt;

    public GoalManager(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago, GameSave save)
    {
        _logger = logger;
        _harmony = harmony;
        _archipelago = archipelago;
        _save = save;
        _completion = new GoalCompletionTracker(archipelago.SlotData.Goal, archipelago.SlotData.GraveyardGoalRating);
        _current = this;
    }

    public void ConnectionChanged()
    {
        _completion.ConnectionChanged();
        _nextCheckAt = 0;
    }

    // Plugin.Update calls this on the game thread, including offline frames.
    // Polling also covers load/reconnect and later quality changes, not just crafts.
    public void CheckGoalCompletion()
    {
        if (!_archipelago.IsConnected) { ConnectionChanged(); return; }
        if (Time.unscaledTime < _nextCheckAt) return;
        _nextCheckAt = Time.unscaledTime + 1;
        try
        {
            var game = MainGame.me;
            var ready = ReferenceEquals(game?.save, _save) && MainGame.game_started && !MainGame.game_starting &&
                !LoadingGUI.is_shown && game?.world_root != null && game.player != null;
            float? rating = null;
            if (ready && _archipelago.SlotData.Goal == Goal.Graveyard && _archipelago.SlotData.GraveyardGoalRating != 0)
            {
                var zone = WorldZone.GetZoneByID("graveyard", null_is_error: false);
                if (zone != null) rating = zone.GetTotalQuality();
            }
            _completion.Check(_archipelago.IsConnected, ready, _save.completed_one_time_crafts, rating,
                () => _archipelago.ReportGoalCompletion());
        }
        catch (Exception ex)
        {
            // Completion is retryable. A missing zone or network failure must
            // not tear down a working save or flood its log every frame.
            if (Time.unscaledTime < _nextFailureLogAt) return;
            _nextFailureLogAt = Time.unscaledTime + 10;
            _logger.LogWarning($"Could not report Archipelago completion; will retry: {ex.Message}");
        }
    }

    public void InjectGoalMethods()
    {
        // Install independently of the current goal. Switching from Graveyard
        // to Portal in one process must not skip this already initialized patch set.
        var original = AccessTools.Method(typeof(CraftComponent), nameof(CraftComponent.CraftAsPlayer));
        var postfix = AccessTools.Method(typeof(CraftComponentCraftAsPlayerPatch), nameof(CraftComponentCraftAsPlayerPatch.Postfix));
        if (Harmony.GetPatchInfo(original)?.Postfixes.Any(patch => patch.owner == _harmony.Id && patch.PatchMethod == postfix) == true)
            return;
        _harmony.Patch(original,
            prefix: new HarmonyMethod(typeof(CraftComponentCraftAsPlayerPatch), nameof(CraftComponentCraftAsPlayerPatch.Prefix)),
            postfix: new HarmonyMethod(postfix));
    }

    public static void CheckPortalGoalCompletion() => _current?.CheckGoalCompletion();

    public void Dispose()
    {
        if (ReferenceEquals(_current, this)) _current = null;
    }
}
