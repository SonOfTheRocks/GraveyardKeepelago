using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class NpcIdentityTests
{
    [Fact]
    public void ExistingMissChainGrantUsesHerNativeIdForBothRewardAndPreview()
    {
        var definition = ClientContractLoader.LoadDefault().Items["ITEM_MISS_CHAIN_HAPPINESS_10"];
        // Keep the historical shared contract compatible with existing seeds.
        Assert.Equal("npc_mrs_chain", definition.Grant.NpcID);
        var actions = new FakePlayerActions();
        var item = new GKRelation(definition.Grant.NpcID, definition.Grant.Amount, actions);
        item.Apply();
        Assert.Equal("npc_mrs chain", actions.LastRelationNpcId);
        Assert.Equal(10, actions.LastRelationAmount);
        item.WithAmount(20).Apply();
        Assert.Equal("npc_mrs chain", actions.LastRelationNpcId);
        Assert.Equal(20, actions.LastRelationAmount);
        var preview = ItemNotification.Create(definition.Name, definition.Grant, null!);
        Assert.Equal(new[] { ("relation", "npc_mrs chain") }, preview.Entries);
        Assert.Equal("char_npc_mrs chain", ItemNotification.PortraitSpriteName(preview.Entries[0].ID, null!));
    }

    [Theory]
    [InlineData("npc_mrs chain")]
    [InlineData("npc_mrs_chain_dlc")]
    [InlineData("npc_mrs_chain_young")]
    [InlineData("npc_bishop")]
    [InlineData("crafting_skull_3")]
    public void NativeIdsAndOtherNpcsRemainUnchanged(string id)
        => Assert.Equal(id, NpcIdentity.Canonical(id));
}
