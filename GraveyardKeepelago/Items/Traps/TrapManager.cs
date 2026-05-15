using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Archipelago;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps
{
    public class TrapManager : ITrapManager
    {
        private const string MOBS = "Monsters";

        private static ILogger _logger;
        private readonly Harmony _harmony;
        private static GKArchipelagoClient _archipelago;
        public readonly TrapExecutor TrapExecutor;
        private readonly Random _random;

        private readonly Dictionary<string, Action> _traps;

        private readonly ConcurrentQueue<QueuedItemTrap> _queuedTraps;
        private object _trapLock = new();

        public TrapManager(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago, TrapExecutor trapExecutor)
        {
            _logger = logger;
            _harmony = harmony;
            _archipelago = archipelago;
            TrapExecutor = trapExecutor;
            _random = new Random(MainGame.me.save.dungeon_seed);
            
            _traps = new Dictionary<string, Action>();
            RegisterTraps();
            _queuedTraps = new ConcurrentQueue<QueuedItemTrap>();
            
            //_harmony.Patch()
        }

        public virtual bool IsTrap(string itemName)
        {
            return _traps.ContainsKey(itemName);
        }

        public virtual bool CanGetTrappedRightNow()
        {
            // TODO
            return false;
        }

        public string ExecuteRandomTrapImmediately()
        {
            var trapNames = _traps.Keys.Where(x => x.EndsWith(" Trap") && !x.Contains("_")).Distinct().OrderBy(x => x)
                .ToArray();
            var randomIndex = _random.Next(0, trapNames.Length);
            var randomTrap = trapNames[randomIndex];
            ExecuteTrapImmediately(randomTrap);
            return randomTrap;
        }

        public virtual bool TryExecuteTrapImmediately(string trapName)
        {
            if (!CanGetTrappedRightNow())
                return false;

            ExecuteTrapImmediately(trapName);
            return true;
        }

        public void ExecuteTrapImmediately(string trapName)
        {
            _queuedTraps.Enqueue(new QueuedItemTrap(trapName, _traps[trapName]));
        }

        public void DequeueTrap()
        {
            if (!CanGetTrappedRightNow())
                return;

            if (_queuedTraps.IsEmpty)
                return;

            if (!_queuedTraps.TryDequeue(out var trap))
                return;

            lock (_trapLock)
            {
                _logger.LogDebug($"Executing Trap {trap.Name}");
                trap.ExecuteNow();
            }
        }

        private void RegisterTraps()
        {
            _traps.Add(MOBS, TrapExecutor.SpawnMonster);

            RegisterTrapsWithTrapSuffix();
            RegisterTrapsWithDifferentSpace();
        }

        private void RegisterTrapsWithDifferentSpace()
        {
            foreach (var trapName in _traps.Keys.ToArray())
            {
                var differentSpacedTrapName = trapName.Replace(" ", "_");
                if (differentSpacedTrapName != trapName)
                    _traps.Add(differentSpacedTrapName, _traps[trapName]);
            }
        }

        private void RegisterTrapsWithTrapSuffix()
        {
            foreach (var trapName in _traps.Keys.ToArray())
            {
                var trapWithSuffix = $"{trapName} Trap";
                _traps.Add(trapWithSuffix, _traps[trapName]);
            }
        }
    }
}
