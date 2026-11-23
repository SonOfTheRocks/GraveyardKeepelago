using System;
using UnityEngine;

namespace GraveyardKeepelago.Items.Traps;

internal static class NativeMonsterTrap
{
    // No native objects are created until the returned action is invoked.
    public static Action Prepare(string id)
    {
        var trap = MonsterTrapDefinition.Get(id);
        var player = MainGame.me?.player;
        if (player == null || player.transform.parent == null ||
            GameBalance.me.GetDataOrNull<ObjectDefinition>(trap.MonsterID) == null)
            throw new InvalidOperationException("Cannot resolve monster trap player, world or definition.");
        var parent = player.transform.parent;
        var origin = player.transform.position;
        var positions = MonsterSpawnPositions.Find(origin.x, origin.y, trap.Count,
            (x, y) => Physics2D.OverlapPoint(new Vector2(x, y), 1) != null);
        return () =>
        {
            foreach (var position in positions)
            {
                // Standalone native monsters: no custom spawner, lifetime or save filtering.
                var monster = WorldMap.SpawnWGO(parent, trap.MonsterID,
                    new Vector3(position.X, position.Y, origin.z));
                if (monster == null)
                    throw new InvalidOperationException("Native monster spawn returned no object.");
            }
        };
    }
}
