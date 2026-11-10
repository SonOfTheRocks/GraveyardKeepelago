using System;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps;

public sealed class MonsterSpawner
{
    private readonly ILogger _logger;
    public MonsterSpawner(ILogger logger) => _logger = logger;

    public void SpawnMonster(MonsterTrapDefinition trap)
    {
        var player = MainGame.me.player;
        if (player == null || string.IsNullOrEmpty(trap.MonsterID) ||
            GameBalance.me.GetDataOrNull<ObjectDefinition>(trap.MonsterID) == null)
            throw new InvalidOperationException("Monster trap cannot resolve a player or monster definition.");

        var runtime = TrapMonsterRuntime.Current ??
            throw new InvalidOperationException("Monster trap lifetime tracking is unavailable.");
        // Resolve the whole group before spawning. Track every successful partial spawn.
        var origin = player.transform.position;
        var positions = MonsterSpawnPositions.Find(origin.x, origin.y, trap.Count,
            (x, y) => UnityEngine.Physics2D.OverlapPoint(new UnityEngine.Vector2(x, y), 1) != null);
        foreach (var position in positions)
        {
            var monster = WorldMap.SpawnWGO(player.transform.parent, trap.MonsterID,
                new UnityEngine.Vector3(position.X, position.Y, origin.z));
            runtime.Register(monster);
        }
        _logger.LogDebug($"{trap.Name} spawned {trap.Count} {trap.MonsterID} near the player.");
    }
}
