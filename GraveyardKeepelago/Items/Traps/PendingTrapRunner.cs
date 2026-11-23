using System;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Items.Traps;

// One runner per bound save. All operations, including preparation, run on Unity's thread.
internal sealed class PendingTrapRunner
{
    private readonly SaveProgress _progress;
    private readonly Action _store;
    private readonly Func<string, Action> _prepare;
    private readonly Action<string> _logError;
    private float _nextAt;
    private string _lastError;
    private bool _updating;

    public PendingTrapRunner(SaveProgress progress, Action store, Func<string, Action> prepare,
        Action<string> logError, float now)
    {
        _progress = progress;
        _store = store;
        _prepare = prepare;
        _logError = logError;
        _nextAt = now + 5f;
    }

    public void Update(float now, Func<bool> ready)
    {
        if (_updating) return;
        if (!ready())
        {
            _nextAt = Math.Max(_nextAt, now + 2f);
            return;
        }
        if (now < _nextAt || _progress.Data.PendingTraps.Count == 0) return;
        _updating = true;
        try
        {
            var queue = _progress.Data.PendingTraps;
            var id = queue[0];
            // Preparation must have no world effects: safe failures can retry.
            var spawn = _prepare(id) ?? throw new InvalidOperationException("Missing monster spawn action.");
            if (!ready())
            {
                _nextAt = Math.Max(_nextAt, now + 2f);
                return;
            }
            queue.RemoveAt(0);
            try { _store(); }
            catch { queue.Insert(0, id); throw; }
            _nextAt = now + 10f;
            try
            {
                // Once native spawning starts, even a thrown call may have created a monster.
                spawn();
                _lastError = null;
            }
            catch (Exception ex)
            {
                Report($"Trap '{id}' consumed after an uncertain or partial spawn; it will not be repeated: {ex}");
            }
        }
        catch (Exception ex)
        {
            _nextAt = now + 10f;
            Report($"Pending monster trap delayed before spawning: {ex}");
        }
        finally { _updating = false; }
    }

    private void Report(string message)
    {
        if (_lastError != message) _logError(message);
        _lastError = message;
    }
}
