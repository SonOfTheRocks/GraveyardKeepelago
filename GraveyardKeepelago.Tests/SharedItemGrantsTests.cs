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
        var grants = new SharedItemGrants(items, actions.Object, Mock.Of<ILogger>());

        actions.Setup(a => a.TryCompleteFlowScript(It.IsAny<string>())).Returns(true);
        actions.Setup(a => a.TryCompleteTechnologyUnlocks(It.IsAny<IEnumerable<string>>())).Returns(true);

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
                    Assert.Equal(GrantResult.Applied, grants.TryApply(item.Name, stage, traps.Add));
                    AssertUnlocks(actions, grant.Stages[stage].Select(id => (type, id)));
                    actions.Invocations.Clear();
                }

                Assert.Equal(GrantResult.Applied, grants.TryApply(item.Name, grant.Stages.Count, traps.Add));
                actions.VerifyNoOtherCalls();
                continue;
            }

            Assert.Equal(GrantResult.Applied, grants.TryApply(item.Name, 0, traps.Add));
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
                    actions.Verify(a => a.TryCompleteTechnologyUnlocks(grant.TechnologyIDs), Times.Once);
                    break;
                case "flow_script":
                    actions.Verify(a => a.TryCompleteFlowScript(grant.ScriptID), Times.Once);
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
            actions.Object, logger.Object);

        var disabled = items.First(item => item.DLC != "BASE_GAME");
        Assert.Equal(GrantResult.Retry, grants.TryApply(disabled.Name, 0, UnexpectedTrap));
        Assert.Equal(GrantResult.Retry, grants.TryApply("bats Trap", 0, UnexpectedTrap));
        logger.Verify(log => log.LogError(It.Is<string>(message => message.Contains(disabled.Name))), Times.Once);
        actions.VerifyNoOtherCalls();
    }

    [Fact]
    public void AStartedFlowStillWaitsForTheNativeEffect()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var flowItem = Assert.Single(items, item => item.Grant.Type == "flow_script");
        var actions = new Mock<ISharedGrantActions>(MockBehavior.Strict);
        actions.SetupSequence(a => a.TryCompleteFlowScript(flowItem.Grant.ScriptID)).Returns(false).Returns(true);
        var grants = new SharedItemGrants(items, actions.Object, Mock.Of<ILogger>());

        Assert.Equal(GrantResult.Pending, grants.TryApply(flowItem.Name, 0, UnexpectedTrap));
        Assert.Equal(GrantResult.Applied, grants.TryApply(flowItem.Name, 0, UnexpectedTrap));
    }

    [Fact]
    public void ATechnologyUnlockCanWaitForItsAssociatedFlow()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_UNLOCK_ANATOMY"];
        var actions = new Mock<ISharedGrantActions>(MockBehavior.Strict);
        actions.SetupSequence(a => a.TryCompleteTechnologyUnlocks(item.Grant.TechnologyIDs)).Returns(false).Returns(true);
        var grants = new SharedItemGrants([item], actions.Object, Mock.Of<ILogger>());

        Assert.Equal(GrantResult.Pending, grants.TryApply(item.Name, 0, UnexpectedTrap));
        Assert.Equal(GrantResult.Applied, grants.TryApply(item.Name, 0, UnexpectedTrap));
    }

    [Fact]
    public void OnlyAnExplicitlyUndeliveredPhysicalItemCanBeRetried()
    {
        var item = ClientContractLoader.LoadDefault().Items.Values.First(item => item.Grant.Type == "game_item");
        var actions = new Mock<ISharedGrantActions>();
        actions.SetupSequence(a => a.DeliverItem(item.Grant.ItemID))
            .Throws(new UndeliveredPhysicalItemException("Inventory and drop rejected the item."))
            .Throws(new IOException("The native operation may have mutated the game."));
        var grants = new SharedItemGrants([item], actions.Object, Mock.Of<ILogger>());

        Assert.Equal(GrantResult.Retry, grants.TryApply(item.Name, 0, UnexpectedTrap));
        Assert.Throws<IOException>(() => grants.TryApply(item.Name, 0, UnexpectedTrap));
    }

    [Fact]
    public void FailedProgressiveGrantLeavesItsStageAvailableForRetry()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_LEARN_GATHERING_PROGRESSIVE_TREE_FELLING"];
        var actions = new Mock<ISharedGrantActions>();
        var grants = new SharedItemGrants([item], actions.Object, Mock.Of<ILogger>());
        var attempts = 0;
        actions.Setup(a => a.ApplyUnlocks(It.IsAny<IEnumerable<(PlayerUtilities.UnlockType, string)>>()))
            .Callback<IEnumerable<(PlayerUtilities.UnlockType, string)>>(unlocks =>
            {
                Assert.True(ItemProcessor.IsApplyingItem);
                Assert.Equal((Work, "t_wood_big"), Assert.Single(unlocks));
                if (attempts++ == 0)
                    throw new IOException("Temporary grant failure");
            });

        Assert.Throws<IOException>(() => grants.TryApply(item.Name, 1, UnexpectedTrap));
        Assert.False(ItemProcessor.IsApplyingItem);
        Assert.Equal(GrantResult.Applied, grants.TryApply(item.Name, 1, UnexpectedTrap));
        Assert.False(ItemProcessor.IsApplyingItem);
        Assert.Equal(2, attempts);
    }

    private static void UnexpectedTrap(string id) => throw new InvalidOperationException(id);

    private static void AssertUnlocks(Mock<ISharedGrantActions> actions,
        IEnumerable<(PlayerUtilities.UnlockType, string)> expected)
        => actions.Verify(a => a.ApplyUnlocks(It.Is<IEnumerable<(PlayerUtilities.UnlockType, string)>>(
            unlocks => unlocks.SequenceEqual(expected))), Times.Once);
}
