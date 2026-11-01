using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Tests;

public class UnlockPresentationTests
{
    [Theory]
    [InlineData("Merchant", 10, "Merchant\nHappiness +10")]
    [InlineData("Ms. Charm", 20, "Ms. Charm\nHappiness +20")]
    [InlineData("NPC - localized name", 5, "NPC - localized name\nHappiness +5")]
    public void RelationReceiptSeparatesLocalizedNpcNameAndActualHappinessAmount(string npc, int amount, string expected)
    {
        var grant = new ClientContractGrant { Type = "relation", NpcID = "npc_test", Amount = amount };
        var notification = ItemNotification.Create("AP item name", grant, null!);
        Assert.Equal(expected, notification.RelationText(npc));
        grant.Amount = 999;
        Assert.Equal(expected, notification.RelationText(npc)); // Queue keeps the received value.
    }

    [Fact]
    public void PreviewNamesItemOnceAndShowsRecipientOnlyInDescription()
    {
        var reward = new RewardPreview { Item = "Bow", Recipient = "Link", Game = "Another game" };
        Assert.Equal("Bow", RewardPreviewPresentation.Name(reward));
        Assert.Equal("Link · Another game\nSimple and Tasty - Unlock 1",
            RewardPreviewPresentation.Description(reward, "Simple and Tasty - Unlock 1"));
        Assert.DoesNotContain("received", RewardPreviewPresentation.UnlockTitle.ToLowerInvariant());
    }

    [Fact]
    public void PendingPreviewShowsItsLocationWithoutInventingRecipient()
    {
        Assert.Equal("Archipelago …", RewardPreviewPresentation.Name(null!));
        Assert.Equal("Check", RewardPreviewPresentation.Description(null!, "Check"));
    }

    [Fact]
    public void EveryHappinessReceiptCarriesTheAffectedNpcForItsPortrait()
    {
        var rewards = ClientContractLoader.LoadDefault().Items.Values.Where(item => item.Grant.Type == "relation").ToList();
        Assert.NotEmpty(rewards);
        foreach (var reward in rewards)
            // Legacy grant IDs are translated to the game's actual NPC ID.
            Assert.Equal(new[] { ("relation", NpcIdentity.Canonical(reward.Grant.NpcID)) },
                ItemNotification.Create(reward.Name, reward.Grant, null!).Entries);
    }

    [Theory]
    [InlineData("npc_merchant", null, "char_npc_merchant")]
    [InlineData("npc_merchant", "", "char_npc_merchant")]
    [InlineData("npc_variant", "npc_base", "char_npc_base")]
    [InlineData("npc_mrs_chain", null, "char_npc_mrs chain")]
    [InlineData("npc_mrs chain", "", "char_npc_mrs chain")]
    public void PortraitUsesTheNativeNpcOverviewNamingAndAliasRules(string id, string? alias, string expected)
        => Assert.Equal(expected, ItemNotification.PortraitSpriteName(id, alias!));

    [Fact]
    public void ConnectionModalHidesItsBackgroundAndRestoresItOnceOnCloseOrFailure()
    {
        var visible = true;
        var restores = 0;
        var lease = new ModalBackgroundLease(() => visible = false, () => { visible = true; restores++; });
        Assert.False(visible);
        lease.Dispose();
        Assert.True(visible);
        lease.Dispose();
        Assert.Equal(1, restores);
    }
}
