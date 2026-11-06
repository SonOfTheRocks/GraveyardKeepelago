using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Technologies;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public class BuffResearchIconTests
{
    [Theory]
    [InlineData(32, 32, 26, 26, 1, 26, 26)]
    [InlineData(64, 64, 26, 26, 1, 26, 26)]
    [InlineData(16, 16, 26, 26, 1, 16, 16)]
    [InlineData(32, 16, 26, 26, 1, 16, 16)]
    [InlineData(32, 32, 26, 13, 1, 26, 13)]
    [InlineData(32, 32, 26, 26, 2, 32, 32)]
    public void BuffsKeepNativePixelSizeAndNeverExceedTheirIconBox(float boxW, float boxH, float spriteW,
        float spriteH, float pixels, float expectedW, float expectedH)
    {
        var fit = BuffResearchIconGeometry.Fit(boxW, boxH, spriteW, spriteH, pixels, .5f, .5f);
        Assert.Equal(expectedW, fit.Width);
        Assert.Equal(expectedH, fit.Height);
        Assert.Equal(0, fit.X);
        Assert.Equal(0, fit.Y);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.5f)]
    [InlineData(1)]
    public void ResizingPreservesTheIconCenterForEveryPivot(float pivot)
    {
        var fit = BuffResearchIconGeometry.Fit(32, 32, 26, 26, 1, pivot, pivot);
        Assert.Equal((.5f - pivot) * 32, fit.X + (.5f - pivot) * fit.Width);
        Assert.Equal((.5f - pivot) * 32, fit.Y + (.5f - pivot) * fit.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void UnavailableSpriteDimensionsLeaveTheWidgetAlone(float width)
        => Assert.Equal((32f, 32f, 0f, 0f), BuffResearchIconGeometry.Fit(32, 32, width, 26, 1, .5f, .5f));

    [Theory]
    [InlineData("Graveyard Keeper", "permanent_buff", true)]
    [InlineData("Other game", "permanent_buff", false)]
    [InlineData("Graveyard Keeper", "item", false)]
    [InlineData("Graveyard Keeper", "relation", false)]
    public void OnlyLocalPermanentBuffPreviewsAreResized(string game, string type, bool expected)
    {
        var definitions = new Dictionary<string, ClientContractItem>
        { ["Reward"] = new() { Grant = new ClientContractGrant { Type = type } } };
        Assert.Equal(expected, RewardPreviewPresentation.IsLocalPermanentBuff(new RewardPreview
            { Game = game, Item = "Reward" }, "Graveyard Keeper", definitions));
        Assert.False(RewardPreviewPresentation.IsLocalPermanentBuff(null, "Graveyard Keeper", definitions));
        Assert.False(RewardPreviewPresentation.IsLocalPermanentBuff(new RewardPreview
            { Game = game, Item = "Unknown" }, "Graveyard Keeper", definitions));
    }
}
