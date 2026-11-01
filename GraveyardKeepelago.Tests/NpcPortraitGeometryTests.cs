using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NpcPortraitGeometryTests
{
    [Fact]
    public void RelationReceiptReservesFullNativePortraitHeightAndMovesTextClearOfIt()
    {
        var layout = new NpcReceiptLayout(226, 46, 82, 100);
        Assert.Equal(120, layout.RowHeight);
        Assert.Equal(90, layout.FrameWidth);
        Assert.Equal(108, layout.FrameHeight);
        Assert.Equal(116, layout.TextWidth);
        Assert.Equal(140, new NpcReceiptLayout(226, 140, 82, 100).RowHeight);
    }

    [Theory]
    [InlineData(226)]
    [InlineData(300)]
    [InlineData(180)]
    public void BothFramesAndTextStayInsideTheOriginalRowWidth(int width)
    {
        var layout = new NpcReceiptLayout(width, 46, 82, 100);
        Assert.Equal(width, layout.RowWidth);
        Assert.Equal(-width / 2f + 6, layout.FrameCenterX - layout.FrameWidth / 2f);
        Assert.Equal(width / 2f - 6, layout.TextLeft + layout.TextWidth);
        Assert.Equal(8, layout.TextLeft - (layout.FrameCenterX + layout.FrameWidth / 2f));
        Assert.True(layout.FrameHeight < layout.RowHeight);
        Assert.True(layout.TextWidth > 0);
    }

    [Fact]
    public void UnsupportedNarrowRowsFailBeforeOverlappingTheTextAndPortrait()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new NpcReceiptLayout(100, 46, 82, 100));
    [Theory]
    [InlineData(40, 40, 82, 100, 0.4f)]
    [InlineData(20, 40, 82, 100, 20f / 82)]
    [InlineData(82, 100, 82, 100, 1)]
    public void NativePortraitFitsWithinIconWithoutChangingItsProportions(float width, float height,
        float portraitWidth, float portraitHeight, float expected)
    {
        var scale = NpcPortraitGeometry.FitScale(width, height, portraitWidth, portraitHeight);
        Assert.Equal(expected, scale, 5);
        Assert.True(portraitWidth * scale <= width + 0.001f);
        Assert.True(portraitHeight * scale <= height + 0.001f);
    }

    [Theory]
    [InlineData(0, 40, 82, 100)]
    [InlineData(40, 0, 82, 100)]
    [InlineData(40, 40, 0, 100)]
    [InlineData(40, 40, 82, -1)]
    public void InvalidGeometryFallsBackInsteadOfCreatingAnUnboundedPortrait(float width, float height, float portraitWidth, float portraitHeight)
        => Assert.Throws<ArgumentOutOfRangeException>(() => NpcPortraitGeometry.FitScale(width, height, portraitWidth, portraitHeight));
}
