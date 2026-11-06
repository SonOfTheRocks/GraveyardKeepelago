using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Journal;

public static class JournalTabOrder
{
    public static T[] InsertAfter<T>(IEnumerable<T> existing, T anchor, T addition)
    {
        var result = existing.ToList();
        if (result.Contains(addition)) throw new InvalidOperationException("Journal tab already exists.");
        var index = result.IndexOf(anchor);
        if (index < 0) throw new InvalidOperationException("Map tab is missing.");
        result.Insert(index + 1, addition);
        return result.ToArray();
    }
}
