using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Items.Traps;

public interface ITemporaryTrapMonster
{
    long ID { get; }
    bool Exists { get; }
    void Suspend(bool suspended);
    void Remove();
}

public sealed class TrapMonsterContext
{
    public object Save { get; set; }
    public object World { get; set; }
    public object Player { get; set; }
    public object Area { get; set; }
    public bool PlayerAlive { get; set; }
    public bool WorldReady { get; set; }
    public bool PlayerControllable { get; set; }
    public bool WindowOpen { get; set; }
    public bool Talking { get; set; }
    public bool Cinematic { get; set; }
    public bool Protected => !WorldReady || !PlayerControllable || WindowOpen || Talking || Cinematic;
}

// Session-only ownership. Pending trap receipts belong to SaveProgress, never here.
public sealed class TemporaryTrapMonsters
{
    public const float GraceSeconds = 2;
    private readonly Dictionary<long, ITemporaryTrapMonster> _monsters = new();
    private readonly Action<Exception> _onRemovalFailure;
    public TemporaryTrapMonsters(Action<Exception> onRemovalFailure = null) => _onRemovalFailure = onRemovalFailure;
    private object _save, _world, _player, _area;
    private bool _wasProtected;
    private float _resumeAt;
    public int Count => _monsters.Count;
    public bool Suspended { get; private set; }
    public bool Contains(long id) => _monsters.ContainsKey(id);
    public bool CanSerialize(long id) => !Contains(id);
    public bool BlocksCombat(long attacker, long target) => Suspended && (Contains(attacker) || Contains(target));

    public void Add(ITemporaryTrapMonster monster, TrapMonsterContext context, float now)
    {
        Update(context, now);
        if (_monsters.Count == 0)
        { _save = context.Save; _world = context.World; _player = context.Player; _area = context.Area; }
        if (_monsters.ContainsKey(monster.ID)) return;
        _monsters.Add(monster.ID, monster);
        Update(context, now);
    }

    public void Update(TrapMonsterContext context, float now)
    {
        if (_monsters.Count == 0) return;
        if (!context.PlayerAlive || context.Save == null || context.World == null || context.Player == null ||
            !ReferenceEquals(_save, context.Save) || !ReferenceEquals(_world, context.World) ||
            !ReferenceEquals(_player, context.Player) || !ReferenceEquals(_area, context.Area))
        { Clear(); return; }
        foreach (var pair in _monsters.ToArray())
            if (!pair.Value.Exists) _monsters.Remove(pair.Key);
        if (_monsters.Count == 0) { Clear(); return; }
        if (context.Protected)
        { _wasProtected = true; _resumeAt = now + GraceSeconds; }
        else if (_wasProtected)
        { _wasProtected = false; _resumeAt = now + GraceSeconds; }
        Suspended = context.Protected || now < _resumeAt;
        foreach (var monster in _monsters.Values) monster.Suspend(Suspended);
    }

    public void Clear()
    {
        // Retain ownership if a removal fails, so it remains excluded from the save.
        foreach (var pair in _monsters.ToArray())
        {
            try
            {
                if (pair.Value.Exists) pair.Value.Remove();
                _monsters.Remove(pair.Key);
            }
            catch (Exception ex)
            {
                if (_onRemovalFailure == null) throw;
                _onRemovalFailure(ex);
            }
        }
        if (_monsters.Count > 0) return;
        _save = _world = _player = _area = null;
        _wasProtected = Suspended = false;
        _resumeAt = 0;
    }
}
