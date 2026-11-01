using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public class ReceiptIconMappingTests
{
    [Theory]
    [InlineData("ITEM_RED_RESEARCH_POINTS", "red", "techno_point_icon04")]
    [InlineData("ITEM_GREEN_RESEARCH_POINTS", "green", "techno_point_icon03")]
    [InlineData("ITEM_BLUE_RESEARCH_POINTS", "blue", "techno_point_icon02")]
    public void ResearchPointReceiptsUseNativeColoredPointIconsAndApNames(string itemKey, string color, string icon)
    {
        var item = ClientContractLoader.LoadDefault().Items[itemKey];
        var receipt = ItemNotification.Create(item.Name, item.Grant, null!);
        Assert.Equal(new[] { ("research_points", color) }, receipt.Entries);
        Assert.Equal(icon, ItemNotification.ResearchPointSpriteName(color));
        Assert.Equal(item.Name, receipt.EntryName("research_points", color));
    }

    [Fact]
    public void SmithingKeepsItsApNameRatherThanCallingTheReceivedItemIron()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_UNLOCK_SMITHING"];
        var receipt = ItemNotification.Create(item.Name, item.Grant, null!);
        Assert.Equal(new[] { ("technology", "Iron") }, receipt.Entries);
        Assert.Equal("Unlock: Smithing", receipt.EntryName("technology", "Iron"));
    }

    [Theory]
    [InlineData("ITEM_UNLOCK_ZOMBIE_BASICS")]
    [InlineData("ITEM_UNLOCK_ZOMBIE_WORKSTATIONS")]
    public void GroupsWithoutNativeTechnologyIdsHaveAnExplicitApIconEntry(string key)
    {
        var item = ClientContractLoader.LoadDefault().Items[key];
        var receipt = ItemNotification.Create(item.Name, item.Grant, null!);
        Assert.Equal(new[] { ("ap_item", item.Name) }, receipt.Entries);
        Assert.Equal(item.Name, receipt.EntryName("ap_item", ""));
    }

    [Fact]
    public void UnknownResearchColorDoesNotInventANativeSprite()
        => Assert.Null(ItemNotification.ResearchPointSpriteName("purple"));

    [Fact]
    public void CasualPrayerUsesThePrayerBookWithAnEmptySymbolCircle()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_CASUAL_PRAYER"];
        var receipt = ItemNotification.Create(item.Name, item.Grant, null!);
        Assert.Equal(new[] { ("item", "preach_1") }, receipt.Entries);
        Assert.Equal("i_b_empty", ItemNotification.ItemSpriteName(receipt.Entries[0].ID, "i_preach_1"));
    }

    [Theory]
    [InlineData("preach_2", "i_b_faith")]
    [InlineData("preach_1:1", "i_b_faith")]
    [InlineData("town_pass", "i_town_pass")]
    public void OtherItemsKeepTheirNativeIcon(string item, string icon)
        => Assert.Equal(icon, ItemNotification.ItemSpriteName(item, icon));
}
