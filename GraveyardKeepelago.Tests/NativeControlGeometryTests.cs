using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeControlGeometryTests
{
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
    public void SwitchAndKnobFitTheRowAndStateLabelStaysAdjacent(float scale)
    {
        var bounds = new NativeControlGeometry(204, 24, 16, scale, scale);
        Assert.True(bounds.SwitchHeight * scale <= 24);
        Assert.True(bounds.KnobOffX - bounds.KnobSize / 2 > 0);
        Assert.True(bounds.KnobOnX + bounds.KnobSize / 2 < bounds.SwitchWidth);
        Assert.True(bounds.KnobOnX > bounds.KnobOffX);
        Assert.InRange(bounds.StateLeft - bounds.SwitchWidth, 1, bounds.SwitchHeight);
        Assert.True(bounds.StateLeft < bounds.Width / 2);
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
