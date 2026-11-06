using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeTitleGeometryTests
{
    [Fact]
    public void ShortConnectionTitleRetainsTheNativeOptionsBandWidth()
    {
        var title = new NativeTitleGeometry(90, 155, 16, 262);
        Assert.Equal(155, title.BandWidth);
        Assert.Equal(90, title.TextWidth);
    }

    [Fact]
    public void ConnectionTitleExpandsBandWithoutShrinkingTheText()
    {
        var title = new NativeTitleGeometry(188, 155, 16, 262);
        Assert.Equal(220, title.BandWidth);
        Assert.Equal(188, title.TextWidth);
        Assert.True(title.BandWidth <= 262);
    }

    [Fact]
    public void NarrowFrameReducesPaddingWhilePreservingTextSize()
    {
        var title = new NativeTitleGeometry(188, 155, 16, 204);
        Assert.Equal(204, title.BandWidth);
        Assert.Equal(188, title.TextWidth);
    }

    [Fact]
    public void OversizeTitleFailsClearlyInsteadOfScalingTheBitmapFont()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NativeTitleGeometry(280, 155, 16, 262));
    }
}
