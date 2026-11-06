using System;
using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Goals;

// Save-local progress is read afresh: receipts and completion packets can arrive
// on different connections, and completed native crafts already survive reloads.
public sealed class GoalCompletionTracker
{
    private readonly Goal _goal;
    private readonly int? _graveyardRating;
    private bool _reported;

    public GoalCompletionTracker(Goal goal, int? graveyardRating)
    {
        if (goal != Goal.Portal && goal != Goal.Graveyard)
            throw new ArgumentOutOfRangeException(nameof(goal));
        if (goal == Goal.Graveyard && (!graveyardRating.HasValue || graveyardRating < 0 || graveyardRating > 2484))
            throw new ArgumentOutOfRangeException(nameof(graveyardRating));
        _goal = goal;
        _graveyardRating = graveyardRating;
    }

    public void ConnectionChanged() => _reported = false;

    public bool Check(bool connected, bool worldReady, ICollection<string> completedCrafts,
        float? graveyardRating, Action report)
    {
        if (!connected) { ConnectionChanged(); return false; }
        if (!worldReady || _reported) return false;
        var complete = _goal == Goal.Portal
            ? completedCrafts != null && completedCrafts.Contains("craft_barrel") && completedCrafts.Contains("craft_emitter")
            : _graveyardRating == 0 || graveyardRating.HasValue && graveyardRating.Value >= _graveyardRating.Value;
        if (!complete) return false;
        report(); // A failed send remains retryable; only acknowledge successful calls.
        _reported = true;
        return true;
    }
}
