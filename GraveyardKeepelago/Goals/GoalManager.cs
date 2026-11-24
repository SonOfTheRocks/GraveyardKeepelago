using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Goals;

// Driven only from Plugin.Update on the Unity thread. No session-specific Harmony hooks.
internal sealed class GoalManager
{
    private Task _send;
    private bool _reported;
    private float _nextCheck;

    internal static bool IsComplete(Goal goal, int target, ICollection<string> completedCrafts, float? rating)
        => goal switch
        {
            Goal.Portal => completedCrafts != null && completedCrafts.Contains("craft_barrel") &&
                completedCrafts.Contains("craft_emitter"),
            Goal.Graveyard => target >= 0 && target <= 2484 && rating.HasValue &&
                !float.IsInfinity(rating.Value) && rating.Value >= target,
            _ => false,
        };

    internal void Update(float now, Func<bool> isReady, Func<bool> isComplete, Func<Task> send, Action<string> log)
    {
        if (_send != null && _send.IsCompleted)
        {
            _reported = _send.Status == TaskStatus.RanToCompletion;
            if (!_reported) log($"Goal transmission failed; retrying: {_send.Exception?.GetBaseException().Message ?? "cancelled"}");
            _send = null;
            _nextCheck = now + 5;
        }
        if (_reported || _send != null || now < _nextCheck || !isReady()) return;
        _nextCheck = now + 1;
        try
        {
            if (!isComplete() || !isReady()) return;
            _send = send();
        }
        catch (Exception ex)
        {
            _nextCheck = now + 5;
            log($"Goal check or transmission failed; retrying: {ex.Message}");
        }
    }
}
