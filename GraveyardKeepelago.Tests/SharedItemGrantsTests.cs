using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Logic;
using KaitoKid.Utilities.Interfaces;
using Moq;
using static GraveyardKeepelago.GameModifications.PlayerUtilities.UnlockType;

namespace GraveyardKeepelago.Tests;

public sealed class SharedItemGrantsTests
{
    [Fact]
    public void EveryEmbeddedItemRoutesItsExactNameAndPayload()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var actions = new Mock<ISharedGrantActions>();
        var traps = new List<string>();
        var grants = new SharedItemGrants(items, actions.Object, id => { traps.Add(id); return true; }, Mock.Of<ILogger>());

        foreach (var item in items)
        {
            var grant = item.Grant;
            traps.Clear();
            actions.Invocations.Clear();
            if (grant.Stages != null)
            {
                var type = grant.Type == "progressive_craft_unlock" ? Craft : Work;
                for (var stage = 0; stage < grant.Stages.Count; stage++)
                {
                    Assert.True(grants.TryApply(item.Name, stage));
                    AssertUnlocks(actions, grant.Stages[stage].Select(id => (type, id)));
                    actions.Invocations.Clear();
                }

                Assert.True(grants.TryApply(item.Name, grant.Stages.Count));
                actions.VerifyNoOtherCalls();
                continue;
            }

            Assert.True(grants.TryApply(item.Name, 0));
            switch (grant.Type)
            {
                case "trap":
                    Assert.Equal(grant.TrapID, Assert.Single(traps));
                    break;
                case "game_item":
                    actions.Verify(a => a.DeliverItem(grant.ItemID), Times.Once);
                    break;
                case "craft_unlock":
                    AssertUnlocks(actions, grant.CraftIDs.Select(id => (Craft, id)));
                    break;
                case "work_unlock":
                    AssertUnlocks(actions, grant.WorkIDs.Select(id => (Work, id))
                        .Concat(grant.PerkIDs.Select(id => (Perk, id))));
                    break;
                case "perk_unlock":
                    AssertUnlocks(actions, new[] { (Perk, grant.PerkID) }
                        .Concat(grant.CraftIDs.Select(id => (Craft, id)))
                        .Concat(grant.WorkIDs.Select(id => (Work, id))));
                    break;
                case "permanent_buff":
                    actions.Verify(a => a.ApplyPermanentBuff(grant.BuffID), Times.Once);
                    break;
                case "relation":
                    actions.Verify(a => a.ApplyRelation(grant.NpcID, grant.Amount), Times.Once);
                    break;
                case "research_points":
                    actions.Verify(a => a.AddResearchPoints(grant.Color, grant.Amount), Times.Once);
                    break;
                case "technology_unlock":
                    actions.Verify(a => a.UnlockTechnologies(grant.TechnologyIDs), Times.Once);
                    break;
                case "flow_script":
                    actions.Verify(a => a.RunFlowScript(grant.ScriptID), Times.Once);
                    break;
                default:
                    throw new InvalidDataException($"Unhandled grant '{grant.Type}' in the v9 data.");
            }

            actions.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public void DisabledAndUnknownNamesNeverReachGameActions()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var logger = new Mock<ILogger>();
        var actions = new Mock<ISharedGrantActions>(MockBehavior.Strict);
        var dlcs = new DLCManager(logger.Object, []);
        var grants = new SharedItemGrants(items.Where(item => dlcs.IsContentEnabled(item.DLC)),
            actions.Object, _ => throw new InvalidOperationException(), logger.Object);

        var disabled = items.First(item => item.DLC != "BASE_GAME");
        Assert.False(grants.TryApply(disabled.Name, 0));
        Assert.False(grants.TryApply("bats Trap", 0));
        logger.Verify(log => log.LogError(It.Is<string>(message => message.Contains(disabled.Name))), Times.Once);
        actions.VerifyNoOtherCalls();
    }

    [Fact]
    public void TrapRejectionIsNotASuccessfulGrant()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var grants = new SharedItemGrants(items, Mock.Of<ISharedGrantActions>(), id =>
        {
            Assert.Equal("bats", id);
            return false;
        }, Mock.Of<ILogger>());

        Assert.False(grants.TryApply("Bats Trap", 0));
    }

    [Fact]
    public void FailedProgressiveGrantLeavesItsStageAvailableForRetry()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_LEARN_GATHERING_PROGRESSIVE_TREE_FELLING"];
        var actions = new Mock<ISharedGrantActions>();
        var grants = new SharedItemGrants([item], actions.Object, _ => false, Mock.Of<ILogger>());
        var attempts = 0;
        actions.Setup(a => a.ApplyUnlocks(It.IsAny<IEnumerable<(PlayerUtilities.UnlockType, string)>>()))
            .Callback<IEnumerable<(PlayerUtilities.UnlockType, string)>>(unlocks =>
            {
                Assert.True(ItemProcessor.IsApplyingItem);
                Assert.Equal((Work, "t_wood_big"), Assert.Single(unlocks));
                if (attempts++ == 0)
                    throw new IOException("Temporary grant failure");
            });

        Assert.Throws<IOException>(() => grants.TryApply(item.Name, 1));
        Assert.False(ItemProcessor.IsApplyingItem);
        Assert.True(grants.TryApply(item.Name, 1));
        Assert.False(ItemProcessor.IsApplyingItem);
        Assert.Equal(2, attempts);
    }

    private static void AssertUnlocks(Mock<ISharedGrantActions> actions,
        IEnumerable<(PlayerUtilities.UnlockType, string)> expected)
        => actions.Verify(a => a.ApplyUnlocks(It.Is<IEnumerable<(PlayerUtilities.UnlockType, string)>>(
            unlocks => unlocks.SequenceEqual(expected))), Times.Once);
}
