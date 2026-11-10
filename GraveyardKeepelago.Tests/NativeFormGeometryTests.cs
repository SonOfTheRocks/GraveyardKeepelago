using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeFormGeometryTests
{
    [Theory]
    [InlineData(268, 306)]
    [InlineData(204, 242)]
    public void WindowTracksContentWidthAndPreservesNativeMargins(float contentWidth, float expectedWindowWidth)
    {
        // Runtime-inspected dialog: content 204, window 242. Frame anchors to the window.
        var windowWidth = NativeFormGeometry.WindowWidth(242, 204, contentWidth);
        Assert.Equal(expectedWindowWidth, windowWidth);
        Assert.Equal(38, windowWidth - contentWidth);
    }

    [Theory]
    [InlineData(204, 16)]
    [InlineData(408, 32)]
    public void WiderDialogAddsSpaceForInputWithoutChangingCaptionColumn(float nativeWidth, float fontSize)
    {
        var captionOffset = NativeControlGeometry.InlineControlOffset(nativeWidth, 105, 8);
        var contentWidth = NativeFormGeometry.ContentWidth(nativeWidth, fontSize);
        var originalInputWidth = nativeWidth - captionOffset;
        var widerInputWidth = contentWidth - captionOffset;
        Assert.Equal(4 * fontSize, widerInputWidth - originalInputWidth);
        Assert.True(widerInputWidth > 0);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    public void CompactRowsLeaveSpaceBetweenInputsAndBeforeStatus(float line)
    {
        for (var row = 0; row < 3; row++)
        {
            var controlBottom = NativeFormGeometry.RowTopOffset(row, line) + line * 1.25f;
            var nextControlTop = NativeFormGeometry.RowTopOffset(row + 1, line) - line / 4;
            Assert.True(controlBottom < nextControlTop);
        }
        var lastBottom = NativeFormGeometry.RowTopOffset(3, line) + line * 1.25f;
        Assert.True(lastBottom < NativeFormGeometry.StatusLineCount(4) * line);
        Assert.Equal(10, NativeFormGeometry.StatusLineCount(4));
    }

    [Fact]
    public void LongestCaptionFitsWithoutShrinkingAndAllControlsShareTheColumn()
    {
        var offset = NativeControlGeometry.InlineControlOffset(204, 105, 8);
        Assert.Equal(113, offset);
        Assert.Equal(105, offset - 8);
        Assert.Equal(91, 204 - offset);
    }
}
