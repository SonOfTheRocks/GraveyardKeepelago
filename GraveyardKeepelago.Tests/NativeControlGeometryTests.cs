using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeControlGeometryTests
{
    [Fact]
    public void SelectorTextRetainsNativeBaselineWhenFrameIsCentered()
    {
        // Options prototype: text origin y=1, visible frame spans -9..8.
        var textY = NativeControlGeometry.SelectorTextOriginY(1, -0.5f);
        Assert.Equal(1.5f, textY);
        Assert.Equal(10, textY - -8.5f);
    }

    [Theory]
    [InlineData(204)]
    [InlineData(408)]
    public void InlineSelectorLeavesRoomForCaptionAndKeepsRightEdge(float width)
    {
        var offset = NativeControlGeometry.InlineControlOffset(width);
        var controlWidth = width - offset;
        Assert.True(offset >= width * 0.39f);
        Assert.True(controlWidth > offset);
        Assert.Equal(width, offset + controlWidth);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void InputFrameAndHitAreaCoverTheWholeRowAtEachNativeScale(float scale)
    {
        var bounds = new NativeControlGeometry(204, 24, 16, scale, scale);
        // The label starts at Inset. Frame and expanded native polygon must start at the caption.
        Assert.Equal(0, (bounds.Inset + bounds.FrameLeft) * scale);
        Assert.Equal(204, (bounds.Inset + bounds.FrameRight) * scale);
        Assert.Equal(204, (bounds.TextWidth + 2 * bounds.Inset) * scale);
        Assert.True(bounds.Inset > 0);
        Assert.True(bounds.TextWidth > 0);
        Assert.Equal(MathF.Round(bounds.Inset), bounds.Inset);
    }

    [Fact]
    public void CounterSizedPrototypeBecomesAFullWidthPaddedField()
    {
        var bounds = new NativeControlGeometry(204, 24, 16, 1, 1);
        Assert.Equal(204, bounds.Width);
        Assert.Equal(194, bounds.TextWidth);
        Assert.Equal(5, bounds.Inset);
        Assert.Equal(24, bounds.Height);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void SelectorArrowsStayInsideTheFrameWithSpaceForTheStateLabel(float scale)
    {
        var bounds = new NativeControlGeometry(204, 24, 16, scale, scale);
        const float arrowWidth = 15;
        Assert.Equal(-bounds.Width / 2, bounds.LeftArrowCenter(arrowWidth) - arrowWidth / 2);
        Assert.Equal(bounds.Width / 2, bounds.RightArrowCenter(arrowWidth) + arrowWidth / 2);
        Assert.True(bounds.SelectorTextWidth(arrowWidth) > 0);
        Assert.True(-bounds.SelectorTextWidth(arrowWidth) / 2 > bounds.LeftArrowCenter(arrowWidth) + arrowWidth / 2);
        Assert.True(bounds.SelectorTextWidth(arrowWidth) / 2 < bounds.RightArrowCenter(arrowWidth) - arrowWidth / 2);
    }

    [Theory]
    [InlineData(24, 1)]
    [InlineData(48, 2)]
    public void SelectorBackgroundPreservesOptionsHeightWithinATallerFormRow(float rowHeight, float scale)
    {
        var bounds = new NativeControlGeometry(204, rowHeight, 16, scale, scale);
        var backgroundHeight = bounds.SelectorBackgroundHeight(17);
        Assert.Equal(17, backgroundHeight);
        Assert.Equal(1, (backgroundHeight - 15) / 2);
        Assert.True(backgroundHeight < bounds.Height);
    }

    [Fact]
    public void SelectorBackgroundFitsInsideAShorterRow()
    {
        var bounds = new NativeControlGeometry(204, 14, 16, 1, 1);
        Assert.Equal(14, bounds.SelectorBackgroundHeight(17));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void InvalidScaleFailsBeforeProducingBrokenWidgetGeometry(float scaleX, float scaleY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NativeControlGeometry(204, 24, 16, scaleX, scaleY));
    }
}
