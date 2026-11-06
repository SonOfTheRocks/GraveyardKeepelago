using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Journal;

// All history/UI work runs on the game thread; network callbacks only enqueue records.
public sealed class JournalHistory
{
    public const int Capacity = 2000;
    private readonly List<JournalEntry> _entries = new();
    private readonly HashSet<string> _transfers = new(StringComparer.Ordinal);
    public IReadOnlyList<JournalEntry> Entries => _entries;
    public int Revision { get; private set; }

    public void Load(IEnumerable<JournalEntry> entries)
    {
        _entries.Clear();
        _transfers.Clear();
        foreach (var entry in entries) Add(entry);
        Revision++;
    }

    public bool Add(JournalEntry entry)
    {
        if (entry.TransferKey != null && !_transfers.Add(entry.TransferKey)) return false;
        _entries.Add(entry);
        if (_entries.Count > Capacity)
        {
            var removed = _entries[0];
            _entries.RemoveAt(0);
            if (removed.TransferKey != null) _transfers.Remove(removed.TransferKey);
        }
        Revision++;
        return true;
    }

    public JournalEntry[] Visible(JournalFilter filter, bool includeOtherItems)
        => _entries.Where(entry => entry.Matches(filter, includeOtherItems)).ToArray();

    public int Unread(bool includeOtherItems)
        => _entries.Count(entry => !entry.Read && entry.Matches(JournalFilter.All, includeOtherItems));

    public void MarkRead(IEnumerable<JournalEntry> entries)
    {
        var changed = false;
        foreach (var entry in entries)
            if (!entry.Read) { entry.Read = true; changed = true; }
        if (changed) Revision++;
    }
}

public sealed class JournalScrollPosition
{
    public float Offset { get; private set; }
    public float Maximum { get; private set; }
    public bool AtBottom => Maximum - Offset <= 2;
    public float Normalized => Maximum > 0 ? Offset / Maximum : 0;
    public float VisibleFraction { get; private set; } = 1;

    public void Resize(float contentHeight, float viewportHeight, bool follow, float removedHeight = 0)
    {
        Maximum = Math.Max(0, contentHeight - viewportHeight);
        VisibleFraction = contentHeight > 0 ? Math.Min(1, Math.Max(0, viewportHeight / contentHeight)) : 1;
        Offset = follow ? Maximum : Math.Max(0, Math.Min(Maximum, Offset - removedHeight));
    }

    public void Move(float amount) => Offset = Math.Max(0, Math.Min(Maximum, Offset + amount));
    public void Latest() => Offset = Maximum;
    public void SetNormalized(float value)
    {
        if (float.IsNaN(value)) return;
        Offset = Math.Max(0, Math.Min(1, value)) * Maximum;
    }
}
