using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Journal;

namespace GraveyardKeepelago.Tests;

public class JournalPopupTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, true)]
    public void ClosingDuringTeardownDoesNotCallDestroyedCharacterCallbacks(bool started, bool player, bool character, bool expected)
        => Assert.Equal(expected, JournalLifecyclePolicy.UseGameCallbacks(started, player, character));

    [Theory]
    [InlineData(560, 240, 16)]
    [InlineData(720, 480, 16)]
    [InlineData(1440, 960, 32)]
    [InlineData(384, 360, 10)]
    public void SuggestionsFloatAboveInputInsideContentArea(float width, float height, float line)
    {
        var layout = new JournalLayout(width, height, line, line * 3);
        var popup = new JournalPopupLayout(layout, line);
        Assert.True(popup.Bounds.Bottom > layout.Input.Top);
        Assert.True(popup.Bounds.Top <= layout.History.Top);
        Assert.Equal(layout.Input.X, popup.Bounds.X);
        Assert.Equal(layout.Input.Right, popup.Bounds.Right);
        Assert.InRange(popup.VisibleRows, 1, 6);
        Assert.True(popup.BorderThickness > 0 && popup.BorderThickness < popup.Padding);
        Assert.True(popup.InnerFill.X < popup.Padding);
        Assert.True(popup.InnerFill.Right > popup.Bounds.Width - popup.Padding);
        Assert.True(popup.InnerFill.Top > -popup.Padding);
        Assert.Equal(popup.Bounds.Width - popup.BorderThickness * 2, popup.InnerFill.Width);
    }

    [Theory]
    [InlineData(295, 310, 310, 295)]
    [InlineData(-295, -310, -310, -295)]
    [InlineData(295, 310, 410, 395)]
    [InlineData(-295, -310, -410, -395)]
    [InlineData(590, 620, 820, 790)]
    public void TechnologyOrnamentKeepsItsNativeOffsetFromThePanelRail(float origin, float rail, float targetRail, float expected)
    {
        Assert.Equal(expected, JournalPanelGeometry.CornerPosition(origin, rail, targetRail));
        Assert.Equal(origin - rail, expected - targetRail);
    }

    [Fact]
    public void ArrowSelectionScrollsLongListsAndStopsAtBothEnds()
    {
        var selection = new JournalCompletionSelection();
        selection.Move(-1, 20, 6);
        Assert.Equal(0, selection.Index);
        for (var i = 0; i < 8; i++) selection.Move(1, 20, 6);
        Assert.Equal(8, selection.Index);
        Assert.Equal(3, selection.FirstVisible);
        selection.Select(99, 20, 6);
        Assert.Equal(19, selection.Index);
        Assert.Equal(14, selection.FirstVisible);
        selection.Move(1, 20, 6);
        Assert.Equal(19, selection.Index);
        selection.Select(0, 20, 6);
        Assert.Equal(0, selection.FirstVisible);
    }

    [Fact]
    public void FilteringAndMouseSelectionCannotLeaveAnInvisibleSelection()
    {
        var selection = new JournalCompletionSelection();
        selection.Select(30, 50, 6);
        selection.Select(30, 2, 6);
        Assert.Equal(1, selection.Index);
        Assert.Equal(0, selection.FirstVisible);
        selection.Reset();
        Assert.Equal(0, selection.Index);
        selection.Select(5, 0, 6);
        Assert.Equal(0, selection.Index);
    }

    [Fact]
    public void TimestampGutterMeasuresEveryTimeInsteadOfAssumingZerosAreWidest()
    {
        var seen = new HashSet<string>();
        float Measure(string text)
        {
            seen.Add(text);
            return text.Sum(character => character == '4' ? 9f : 4f);
        }
        var width = JournalTimestampGeometry.RequiredWidth(Measure);
        Assert.Equal(1440, seen.Count);
        Assert.Contains("13:40", seen);
        Assert.Contains("23:59", seen);
        Assert.True(width >= seen.Max(Measure) + 2);
        Assert.True(width > Measure("00:00"));
    }
}
