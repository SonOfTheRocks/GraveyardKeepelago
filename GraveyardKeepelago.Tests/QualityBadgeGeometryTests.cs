using GraveyardKeepelago.GameModifications.UI;
using Archipelago.MultiClient.Net.Enums;

namespace GraveyardKeepelago.Tests;

public class QualityBadgeGeometryTests
{
    [Theory]
    [InlineData(-16, -16, 16, 16, 1, 2, 16, 16)]
    [InlineData(-64, -64, 64, 64, 49, 50, 64, 64)]
    [InlineData(0, 0, 8, 7, 0, 0, 8, 7)]
    public void LargeAtlasCanvasesCannotMakeTheSkullLargerThanTheNativeStar(float left, float bottom, float right,
        float top, float expectedLeft, float expectedBottom, float expectedRight, float expectedTop)
    {
        var reference = QualityBadgeGeometry.StarReference(left, bottom, right, top, 15, 14, 1);
        Assert.Equal((expectedLeft, expectedBottom, expectedRight, expectedTop), reference);
        var fit = QualityBadgeGeometry.FitRendered(left, bottom, right, top,
            reference.Left, reference.Bottom, reference.Right, reference.Top);
        Assert.True((right - left) * fit.Scale <= 15);
        Assert.True((top - bottom) * fit.Scale <= 14);
    }

    [Theory]
    [InlineData(0, 0, 24, 30, 0, 0, 15, 14)]
    [InlineData(40, 32, 64, 62, 60, 70, 75, 84)]
    [InlineData(-88, -96, -64, -66, -68, -58, -53, -44)]
    public void PackedSkullFitsTheActualVisibleStarAndSharesItsTopRight(float left, float bottom, float right,
        float top, float starLeft, float starBottom, float starRight, float starTop)
    {
        var fit = QualityBadgeGeometry.FitRendered(left, bottom, right, top, starLeft, starBottom, starRight, starTop);
        Assert.True(fit.Scale < .5f);
        Assert.True((right - left) * fit.Scale <= starRight - starLeft);
        Assert.True((top - bottom) * fit.Scale <= starTop - starBottom + .00001f);
        Assert.Equal(starRight, right * fit.Scale + fit.X, 4);
        Assert.Equal(starTop, top * fit.Scale + fit.Y, 4);
        Assert.Equal(fit, QualityBadgeGeometry.FitRendered(left, bottom, right, top, starLeft, starBottom, starRight, starTop));
    }

    [Fact]
    public void EmptyRenderedSpritesDoNotProduceInvalidTransforms()
        => Assert.Equal((1f, 0f, 0f), QualityBadgeGeometry.FitRendered(0, 0, 0, 0, 0, 0, 15, 14));

    [Theory]
    [InlineData(0)]
    [InlineData(.5f)]
    [InlineData(1)]
    public void NativeSkullFitsTheExistingBadgeSlotWithoutMovingItsTopRightCorner(float pivot)
    {
        var fit = QualityBadgeGeometry.NativeSkullBounds(15, 14, 24, 30, 1, pivot, pivot);
        Assert.Equal(11, fit.Width);
        Assert.Equal(14, fit.Height);
        Assert.Equal((1 - pivot) * 15, fit.X + (1 - pivot) * fit.Width, 5);
        Assert.Equal((1 - pivot) * 14, fit.Y + (1 - pivot) * fit.Height, 5);
        Assert.Equal(fit, QualityBadgeGeometry.NativeSkullBounds(15, 14, 24, 30, 1, pivot, pivot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void UnavailableNativeSkullDimensionsDoNotCorruptTheSlot(float width)
        => Assert.Equal((15f, 14f, 0f, 0f), QualityBadgeGeometry.NativeSkullBounds(15, 14, width, 30, 1, .5f, .5f));

    [Fact]
    public void TrapBadgeUsesTheNativeSkullSelectedByApFlags()
        => Assert.Equal(TrapBadgeSprite.NativeSpriteName,
            GraveyardKeepelago.Technologies.RewardPreviewPresentation.CategorySpriteName(ItemFlags.Trap));

    [Theory]
    [InlineData(32f, 32f)]
    [InlineData(64f, 64f)]
    [InlineData(24f, 30f)]
    public void SkullWidgetFitsInsideTheNativeStarBox(float width, float height)
    {
        var layout = QualityBadgeGeometry.Fit(width, height, 15, 14, 0.5f, 0.5f);
        Assert.InRange(width * layout.Scale, 0, 15);
        Assert.InRange(height * layout.Scale, 0, 14);
        Assert.True(layout.Scale < 1);
        Assert.Equal(width / height, (width * layout.Scale) / (height * layout.Scale), 5);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1f, 1f)]
    [InlineData(0f, 1f)]
    public void ShrinkingKeepsTheTopRightCornerAtTheStarsPosition(float pivotX, float pivotY)
    {
        var layout = QualityBadgeGeometry.Fit(32, 32, 15, 14, pivotX, pivotY);
        Assert.Equal((1 - pivotX) * 32, layout.X + (1 - pivotX) * 32 * layout.Scale, 5);
        Assert.Equal((1 - pivotY) * 32, layout.Y + (1 - pivotY) * 32 * layout.Scale, 5);
    }

    [Theory]
    [InlineData(true, "i_skull", true)]
    [InlineData(true, "i_skull(Clone)", true)]
    [InlineData(false, "i_skull", false)]
    [InlineData(true, "item_star_3", false)]
    [InlineData(true, "item_star_2", false)]
    [InlineData(true, "item_star_1", false)]
    [InlineData(true, null, false)]
    public void OnlyTheApSkullBadgeNeedsAnOverride(bool apActive, string? name, bool expected)
        => Assert.Equal(expected, QualityBadgeGeometry.IsTrapBadge(apActive, name!));

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidDimensionsLeaveTheNativeTransformAlone(float width)
        => Assert.Equal((1f, 0f, 0f), QualityBadgeGeometry.Fit(width, 32, 15, 14, 0.5f, 0.5f));

    [Fact]
    public void SmallBadgesAreNotEnlargedAndRecalculationFromNativeStateDoesNotAccumulate()
    {
        Assert.Equal((1f, 0f, 0f), QualityBadgeGeometry.Fit(8, 8, 15, 14, 0.5f, 0.5f));
        var first = QualityBadgeGeometry.Fit(32, 32, 15, 14, 0.5f, 0.5f);
        Assert.Equal(first, QualityBadgeGeometry.Fit(32, 32, 15, 14, 0.5f, 0.5f));
    }
}
