using System;
using System.IO;
using GraveyardKeepelago.Progression;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps;

// All calls run on Unity's main thread. Pending IDs and processed receipts live
// in the same save record, so accepting an item and saving its trap are atomic.
public sealed class TrapManager : ITrapManager
{
    private readonly ILogger _logger;
    private readonly SaveProgress _progress;
    private readonly Action _store;
    private readonly Action<string> _executeMonsterTrap;
    private readonly Func<bool> _ready;
    private readonly Func<float> _clock;
    private float _nextTrapAt;
    public const float CooldownSeconds = 10f;

    public TrapManager(ILogger logger, SaveProgress progress, Action store,
        Action<string> executeMonsterTrap, Func<bool> ready, Func<float> clock)
    {
        _logger = logger;
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _executeMonsterTrap = executeMonsterTrap ?? throw new ArgumentNullException(nameof(executeMonsterTrap));
        _ready = ready;
        _clock = clock;
        foreach (var id in progress.PendingTraps)
            MonsterTrapDefinition.Get(id);
        _nextTrapAt = clock() + 5f;
    }

    public bool IsTrap(string name) => name == MonsterTrapDefinition.Bats.Name ||
        name == MonsterTrapDefinition.GreenSlimes.Name || name == MonsterTrapDefinition.Legacy.Name ||
        name == "Monsters" || name == "Monsters_Trap";

    // Legacy handler entry point: acceptance always queues, even while unsafe.
    public bool TryExecuteTrapImmediately(string name)
    {
        if (!IsTrap(name)) return false;
        Enqueue(name == MonsterTrapDefinition.Bats.Name ? MonsterTrapDefinition.Bats.ID :
            name == MonsterTrapDefinition.GreenSlimes.Name ? MonsterTrapDefinition.GreenSlimes.ID : "monsters");
        return true;
    }

    public void Enqueue(string trapID)
    {
        var trap = MonsterTrapDefinition.Get(trapID);
        _progress.PendingTraps.Add(trapID);
        _nextTrapAt = Math.Max(_nextTrapAt, _clock() + 2f);
        // ItemReceiptLedger stores this queue together with the receipt after
        // successful application. Do not persist a half-applied receipt here.
        _logger.LogInfo($"{trap.Name} queued until normal gameplay resumes.");
    }

    public void DequeueTrap()
    {
        if (_progress.PendingTraps.Count == 0) return;
        var now = _clock();
        if (!_ready()) { _nextTrapAt = Math.Max(_nextTrapAt, now + 2f); return; }
        if (now < _nextTrapAt) return;
        var id = _progress.PendingTraps[0];
        _progress.PendingTraps.RemoveAt(0);
        // Commit consumption before invoking the world effect. A partially
        // failing spawn must not be retried and create duplicate monsters.
        try { _store(); }
        catch { _progress.PendingTraps.Insert(0, id); throw; }
        _nextTrapAt = now + CooldownSeconds;
        try
        {
            _executeMonsterTrap(id);
            _logger.LogInfo($"{MonsterTrapDefinition.Get(id).Name} activated.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Trap '{id}' failed and was consumed to avoid replaying a partial spawn: {ex}");
        }
    }
}
