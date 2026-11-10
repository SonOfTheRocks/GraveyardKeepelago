using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Journal;

namespace GraveyardKeepelago.Tests;

public class JournalScrollbarTests
{
    [Fact]
    public void DraggingScrollbarAndOtherScrollingShareOnePosition()
    {
        var scroll = new JournalScrollPosition();
        scroll.Resize(1000, 200, false);
        Assert.Equal(0.2f, scroll.VisibleFraction);
        scroll.SetNormalized(0.25f);
        Assert.Equal(200, scroll.Offset);
        scroll.Move(200);
        Assert.Equal(0.5f, scroll.Normalized);
        scroll.Latest();
        Assert.Equal(1, scroll.Normalized);
        Assert.True(scroll.AtBottom);
    }

    [Fact]
    public void DragIsClampedAndAnEmptyOrShortHistoryHasNoScrollRange()
    {
        var scroll = new JournalScrollPosition();
        scroll.Resize(1000, 200, true);
        scroll.SetNormalized(-1);
        Assert.Equal(0, scroll.Offset);
        scroll.SetNormalized(2);
        Assert.Equal(800, scroll.Offset);
        scroll.SetNormalized(float.NaN);
        Assert.Equal(800, scroll.Offset);
        foreach (var height in new[] { 0, 100, 200 })
        {
            scroll.Resize(height, 200, false);
            scroll.SetNormalized(1);
            Assert.Equal(0, scroll.Maximum);
            Assert.Equal(0, scroll.Normalized);
            Assert.Equal(1, scroll.VisibleFraction);
        }
    }

    [Fact]
    public void NewMessagesPreserveScrollPositionUnlessFollowingLatest()
    {
        var scroll = new JournalScrollPosition();
        scroll.Resize(1000, 200, false);
        scroll.SetNormalized(0.5f);
        scroll.Resize(1800, 200, false);
        Assert.Equal(400, scroll.Offset);
        Assert.Equal(0.25f, scroll.Normalized);
        scroll.Resize(2000, 200, true);
        Assert.Equal(1, scroll.Normalized);
    }

}
