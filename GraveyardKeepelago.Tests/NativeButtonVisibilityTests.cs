using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeButtonVisibilityTests
{
    [Fact]
    public void PanelSizedAnchorOutsideGameCanvasIsRejectedDespiteEnabledObjectsAndCollider()
    {
        // Actual failing native position (930, 510), relative to the game's 960x540 canvas.
        var viewportX = 0.5f + 930f / 960;
        var viewportY = 0.5f + 510f / 540;
        Assert.False(NativeButtonVisibility.IsUsable(viewportX, viewportY, true, true, true));
    }

    [Fact]
    public void AnchorInsideActualScreenWidgetPasses()
    {
        var viewportX = 0.5f + 450f / 960;
        var viewportY = 0.5f + 240f / 540;
        Assert.True(NativeButtonVisibility.IsUsable(viewportX, viewportY, true, true, true));
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void PositionAloneDoesNotProveButtonIsUsable(bool frame, bool icon, bool clickable)
    {
        Assert.False(NativeButtonVisibility.IsUsable(0.96f, 0.94f, frame, icon, clickable));
    }
}
