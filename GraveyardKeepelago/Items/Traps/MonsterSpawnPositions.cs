using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Items.Traps;

public static class MonsterSpawnPositions
{
    // Native movement uses 96 world units per tile. Resolve the complete group
    // before any world effect; blocked directions must never reuse a position.
    public static IReadOnlyList<(float X, float Y)> Find(float originX, float originY, int count,
        Func<float, float, bool> blocked)
    {
        if (count < 1 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
        var positions = new List<(float X, float Y)>();
        var used = new HashSet<int>();
        for (var index = 0; index < count; index++)
        {
            var found = false;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var direction = (index * 2 + attempt) % 8;
                if (used.Contains(direction)) continue;
                var angle = direction * Math.PI / 4;
                var x = originX + (float)Math.Cos(angle) * 96f;
                var y = originY + (float)Math.Sin(angle) * 96f;
                if (blocked(x, y)) continue;
                positions.Add((x, y));
                used.Add(direction);
                found = true;
                break;
            }
            if (!found) throw new InvalidOperationException("No clear nearby position for monster trap.");
        }
        return positions;
    }
}
