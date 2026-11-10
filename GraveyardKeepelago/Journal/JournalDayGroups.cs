using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Journal;

public sealed class JournalDayGroup
{
    public DateTime Date { get; }
    public List<JournalEntry> Entries { get; } = new();
    public JournalDayGroup(DateTime date) => Date = date.Date;
    public string Caption => Date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
}

public static class JournalDayGroups
{
    // Group the filtered arrival order, using the same local timezone as the timestamps.
    public static IEnumerable<JournalDayGroup> Build(IEnumerable<JournalEntry> entries, TimeZoneInfo timezone)
    {
        JournalDayGroup group = null;
        foreach (var entry in entries)
        {
            var date = TimeZoneInfo.ConvertTime(entry.Timestamp, timezone).Date;
            if (group == null || group.Date != date)
            {
                if (group != null) yield return group;
                group = new JournalDayGroup(date);
            }
            group.Entries.Add(entry);
        }
        if (group != null) yield return group;
    }
}
