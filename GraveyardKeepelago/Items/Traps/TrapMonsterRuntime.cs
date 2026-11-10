using System;
using System.Linq;
using System.Collections.Generic;
using NodeCanvas.BehaviourTrees;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;
using UnityEngine;

namespace GraveyardKeepelago.Items.Traps;

// Native adapters run only on the Unity thread. No identity marker is written to disk.
internal sealed class TrapMonsterRuntime : IDisposable
{
    public static TrapMonsterRuntime Current { get; private set; }
    private readonly TemporaryTrapMonsters _monsters;
    private readonly ILogger _logger;
    private float _nextFailureLog;
    public TrapMonsterRuntime(ILogger logger)
    {
        _logger = logger;
        _monsters = new TemporaryTrapMonsters(ex => logger.LogError($"Cannot dismiss a temporary trap monster: {ex}"));
        Current = this;
    }
    public bool CanSerialize(long id) => _monsters.CanSerialize(id);
    public void Register(WorldGameObject monster) => _monsters.Add(new NativeMonster(monster), Context(), Time.unscaledTime);
    public void Update() => Refresh();
    private bool Refresh()
    {
        if (_monsters.Count == 0) return true;
        try
        {
            var suspended = _monsters.Suspended;
            var count = _monsters.Count;
            _monsters.Update(Context(), Time.unscaledTime);
            if (count != _monsters.Count || suspended != _monsters.Suspended)
                _logger.LogDebug($"Temporary trap monsters: {_monsters.Count} active, suspended={_monsters.Suspended}.");
            return true;
        }
        catch (Exception ex)
        {
            // Fail closed for our enemies; do not interrupt native combat, dialogue or AP receipts.
            if (Time.unscaledTime >= _nextFailureLog)
            {
                _nextFailureLog = Time.unscaledTime + 5;
                _logger.LogError($"Temporary trap monster protection failed; their updates/hits remain blocked: {ex}");
            }
            return false;
        }
    }
    public bool CanUpdate(WorldGameObject monster)
    {
        if (!_monsters.Contains(monster.unique_id)) return true;
        if (!Refresh()) return false; // Also catches transitions before Plugin.Update in this frame.
        return !monster.is_removed && !_monsters.Suspended;
    }
    public bool CanHit(WorldGameObject attacker, WorldGameObject target)
    {
        if (attacker == null || target == null) return true;
        var tracked = _monsters.Contains(attacker.unique_id) || _monsters.Contains(target.unique_id);
        if (!tracked) return true;
        if (!Refresh()) return false;
        return !attacker.is_removed && !target.is_removed &&
            attacker.gameObject.activeInHierarchy && target.gameObject.activeInHierarchy &&
            !_monsters.BlocksCombat(attacker.unique_id, target.unique_id);
    }
    public void Clear()
    {
        var count = _monsters.Count;
        _monsters.Clear();
        if (count > 0) _logger.LogDebug($"Dismissed {count - _monsters.Count} temporary trap monsters.");
    }
    public void Dispose()
    {
        Clear();
        if (ReferenceEquals(Current, this)) Current = null;
    }

    private static TrapMonsterContext Context()
    {
        var game = MainGame.me;
        var gui = GUIElements.me;
        var player = game?.player;
        var character = game?.player_char;
        var context = new TrapMonsterContext
        {
            Save = game?.save, World = game?.world_root, Player = player,
            Area = character?.player?.current_zone,
            PlayerAlive = player != null && !player.is_dead && player.hp > 0,
            WorldReady = MainGame.game_started && !MainGame.game_starting && !MainGame.paused &&
                !MainGame.disable_all_game && !LoadingGUI.is_shown && gui != null && !GUIElements.gui_is_initializing,
            PlayerControllable = character != null && character.control_enabled &&
                character.can_be_locally_controlled && !character.player_controlled_by_script,
            WindowOpen = !BaseGUI.all_guis_closed,
        };
        if (gui == null || GUIElements.gui_is_initializing || !MainGame.game_started) return context;
        context.Talking = gui.speech_bubble.gameObject.activeInHierarchy ||
                gui.multi_answer.gameObject.activeInHierarchy || gui.corner_talk.is_shown ||
                SpeechBubbleGUI.all.Values.Any(bubble => bubble != null && bubble.gameObject.activeInHierarchy);
        context.Cinematic = gui.cinematic_text.is_shown || CameraTools.cinematics.IsPlaying ||
                CameraTools.letterbox.Amount > 0;
        return context;
    }

    private sealed class NativeMonster : ITemporaryTrapMonster
    {
        private readonly WorldGameObject _monster;
        private Rigidbody2D _body;
        private readonly List<BehaviourTreeOwner> _pausedTrees = new();
        private bool _suspended, _originalDontUpdate, _originalSimulated;
        public NativeMonster(WorldGameObject monster)
        {
            _monster = monster; ID = monster.unique_id; _body = monster.GetComponent<Rigidbody2D>();
        }
        public long ID { get; }
        public bool Exists => _monster != null;
        public void Suspend(bool suspended)
        {
            if (!Exists || _monster.is_removed) return;
            if (_body == null)
            {
                _body = _monster.GetComponent<Rigidbody2D>();
                if (_body != null && _suspended)
                {
                    _originalSimulated = _body.simulated;
                    _body.velocity = Vector2.zero;
                    _body.angularVelocity = 0;
                }
            }
            if (_suspended == suspended)
            {
                if (suspended)
                {
                    _monster.dont_update = true;
                    if (_body != null) _body.simulated = false;
                    PauseTrees(); // An owner may have started after the spawn frame.
                }
                return;
            }
            if (suspended)
            {
                _originalDontUpdate = _monster.dont_update;
                _monster.dont_update = true;
                PauseTrees();
                if (_body != null)
                {
                    _originalSimulated = _body.simulated;
                    _body.velocity = Vector2.zero;
                    _body.angularVelocity = 0;
                    _body.simulated = false;
                }
            }
            else
            {
                _monster.dont_update = _originalDontUpdate;
                if (_body != null) _body.simulated = _originalSimulated;
                foreach (var tree in _pausedTrees)
                    if (tree != null && tree.isPaused && !_monster.is_dead) tree.StartBehaviour();
                _pausedTrees.Clear();
            }
            _suspended = suspended;
        }
        private void PauseTrees()
        {
            foreach (var tree in _monster.GetComponentsInChildren<BehaviourTreeOwner>(true))
                if (tree != null && tree.isRunning)
                { tree.PauseBehaviour(); if (!_pausedTrees.Contains(tree)) _pausedTrees.Add(tree); }
        }
        public void Remove()
        {
            if (!Exists) return;
            // Dismissal is not a kill and must not grant combat rewards or inventory drops.
            try
            {
                if (!_monster.is_removed)
                {
                    // Native SpawnWGO schedules a ground redraw after 0.5s. Its managed
                    // callback exits on this flag, even if dismissal precedes that timer.
                    _monster.is_dead = true;
                    _monster.data?.inventory?.Clear();
                    _monster.DestroyMe();
                }
            }
            finally
            {
                if (_monster != null)
                {
                    // Destroy is deferred: detach now so same-frame saving cannot rediscover it.
                    _monster.transform.SetParent(null, true);
                    _monster.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(_monster.gameObject);
                }
            }
        }
    }
}
