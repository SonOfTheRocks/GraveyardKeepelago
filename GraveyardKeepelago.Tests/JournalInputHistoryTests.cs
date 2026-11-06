using GraveyardKeepelago.Journal;

namespace GraveyardKeepelago.Tests;

public class JournalInputHistoryTests
{
    [Fact]
    public void UpAndDownRecallInputsAndRestoreTheDraft()
    {
        var history = new JournalInputHistory();
        history.Record("hello");
        history.Record("!hint Furnace");
        Assert.Equal("!hint Furnace", history.Move(-1, "unfinished chat"));
        Assert.Equal("hello", history.Move(-1, "!hint Furnace"));
        Assert.Equal("hello", history.Move(-1, "hello"));
        Assert.Equal("!hint Furnace", history.Move(1, "hello"));
        Assert.Equal("unfinished chat", history.Move(1, "!hint Furnace"));
        Assert.Equal("unfinished chat", history.Move(1, "unfinished chat"));
    }

    [Fact]
    public void EmptyHistoryLeavesTextAloneAndConsecutiveDuplicatesDoNotFillTheHistory()
    {
        var history = new JournalInputHistory();
        Assert.Equal("draft", history.Move(-1, "draft"));
        history.Record(" ");
        history.Record(" !help ");
        history.Record("!help");
        Assert.Equal(1, history.Count);
        Assert.Equal("!help", history.Move(-1, ""));
        Assert.Equal("", history.Move(1, "!help"));
    }

    [Fact]
    public void EditingRecalledTextPreservesItAsANewDraft()
    {
        var history = new JournalInputHistory();
        history.Record("!hint Furnace");
        Assert.Equal("!hint Furnace", history.Move(-1, ""));
        Assert.Equal("!hint Furnace", history.Move(-1, "!hint Furnace II"));
        Assert.Equal("!hint Furnace II", history.Move(1, "!hint Furnace"));
    }

    [Fact]
    public void HistoryIsBoundedAndReopeningResetsNavigationWithoutDiscardingSentLines()
    {
        var history = new JournalInputHistory();
        for (var i = 0; i <= JournalInputHistory.Capacity; i++) history.Record("line " + i);
        Assert.Equal(JournalInputHistory.Capacity, history.Count);
        var text = "draft";
        for (var i = 0; i < JournalInputHistory.Capacity + 5; i++) text = history.Move(-1, text);
        Assert.Equal("line 1", text);
        history.Reset();
        Assert.Equal("line 100", history.Move(-1, "new draft"));
        Assert.Equal("new draft", history.Move(1, "line 100"));
    }
}
