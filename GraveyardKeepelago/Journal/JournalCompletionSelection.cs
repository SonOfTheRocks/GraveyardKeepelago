using System;

namespace GraveyardKeepelago.Journal;

public sealed class JournalCompletionSelection
{
    public int Index { get; private set; }
    public int FirstVisible { get; private set; }
    public void Reset() { Index = FirstVisible = 0; }
    public void Select(int index, int count, int visibleRows)
    {
        if (count == 0) { Reset(); return; }
        Index = Math.Max(0, Math.Min(count - 1, index));
        if (Index < FirstVisible) FirstVisible = Index;
        if (Index >= FirstVisible + visibleRows) FirstVisible = Index - visibleRows + 1;
        FirstVisible = Math.Max(0, Math.Min(FirstVisible, Math.Max(0, count - visibleRows)));
    }
    public void Move(int delta, int count, int visibleRows) => Select(Index + delta, count, visibleRows);
}
