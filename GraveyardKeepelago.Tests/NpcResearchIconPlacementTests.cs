using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NpcResearchIconPlacementTests
{
    [Theory]
    [InlineData(25, 2.5f)]
    [InlineData(32, 3)]
    [InlineData(40, 4)]
    [InlineData(48, 5)]
    public void NpcMovesDownByHalfThePreviousOffsetForItsIconSize(float height, float distance)
    {
        const float original = 3;
        var positioned = NpcResearchIconPlacement.PositionY(original, height, true, true);
        Assert.Equal(original - distance, positioned);
        Assert.True(positioned < original);
        Assert.True(original - positioned < height / 8);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void DialogPortraitsAndNonNpcRewardsKeepTheirNativePosition(bool tree, bool npc)
        => Assert.Equal(7, NpcResearchIconPlacement.PositionY(7, 40, tree, npc));

    [Fact]
    public void RedrawsDoNotAccumulateOffsetAndReusedRowsReturnToTheirOriginalPosition()
    {
        const float original = 5;
        var firstDraw = NpcResearchIconPlacement.PositionY(original, 40, true, true);
        for (var redraw = 0; redraw < 20; redraw++)
            Assert.Equal(firstDraw, NpcResearchIconPlacement.PositionY(original, 40, true, true));
        Assert.Equal(original, NpcResearchIconPlacement.PositionY(original, 40, true, false));
        Assert.Equal(firstDraw, NpcResearchIconPlacement.PositionY(original, 40, true, true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidIconSizeDoesNotMoveTheCharacter(float height)
        => Assert.Equal(4, NpcResearchIconPlacement.PositionY(4, height, true, true));
}
