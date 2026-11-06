using Archipelago.MultiClient.Net.Enums;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Tests;

public class TechnologyCategoryBadgeTests
{
    [Theory]
    [InlineData(0, "item_star_1")]
    [InlineData(1, "item_star_3")]
    [InlineData(2, "item_star_2")]
    [InlineData(4, "i_skull")]
    [InlineData(3, "item_star_3")]
    [InlineData(5, "item_star_3")]
    [InlineData(6, "item_star_2")]
    [InlineData(7, "item_star_3")]
    [InlineData(8, "item_star_1")]
    public void ServerFlagsSelectNativeCornerBadgesWithApPrecedence(int bits, string sprite)
        => Assert.Equal(sprite, RewardPreviewPresentation.CategorySpriteName((ItemFlags)bits));

    [Fact]
    public void UnscoutedOrUnknownFlagsDoNotPretendToBeFiller()
    {
        Assert.Null(RewardPreviewPresentation.CategorySpriteName(null));
        Assert.Null(RewardPreviewPresentation.CategorySpriteName(new RewardPreview().Flags));
    }

    [Theory]
    [InlineData(ItemFlags.None, "item_star_1")]
    [InlineData(ItemFlags.Advancement, "item_star_3")]
    [InlineData(ItemFlags.NeverExclude, "item_star_2")]
    [InlineData(ItemFlags.Trap, "i_skull")]
    public void ForeignGameRewardsCarryTheSameCategoryBadgesThroughThePreviewCache(ItemFlags flags, string sprite)
    {
        var cache = new RewardPreviewCache(null!);
        cache.Accept(new[] { new RewardPreview {
            Location = "Test research check", Item = "An item from another game",
            Game = "Another game", Recipient = "Friend", Flags = flags,
        } });
        Assert.Equal(sprite, RewardPreviewPresentation.CategorySpriteName(cache.Get("Test research check").Flags));
    }

    [Fact]
    public void RefreshedAndReusedPreviewsDoNotKeepThePreviousRewardsBadge()
    {
        var cache = new RewardPreviewCache(null!);
        const string location = "Test research check";
        cache.Accept(new[] { new RewardPreview { Location = location, Flags = ItemFlags.Advancement } });
        Assert.Equal("item_star_3", RewardPreviewPresentation.CategorySpriteName(cache.Get(location).Flags));
        cache.Accept(new[] { new RewardPreview { Location = location, Flags = ItemFlags.Trap } });
        Assert.Equal("i_skull", RewardPreviewPresentation.CategorySpriteName(cache.Get(location).Flags));
        cache.Accept(new[] { new RewardPreview { Location = location } });
        Assert.Null(RewardPreviewPresentation.CategorySpriteName(cache.Get(location).Flags));
    }
}
