using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class ItemParserRoutingTests
{
    [Fact]
    public void ProcessEveryContractItem_RoutesAccordingToItsGrant()
    {
        var contract = ClientContractLoader.LoadDefault();
        var actions = new FakePlayerActions();
        var logger = new FakeLogger();
        var registry = new GKItemRegistry();
        new GKItemFactory(actions, contract).BuildAll(registry);
        var processor = new ItemProcessor(logger, registry, new TrapManagerStub());

        foreach (var contractItem in contract.Items.Values)
        {
            actions.Clear();
            logger.ErrorLogs.Clear();

            processor.ProcessItem(CreateReceivedItem(contractItem.Name));

            Assert.Empty(logger.ErrorLogs);
            AssertGrantApplied(contractItem, actions);
        }
    }

    [Theory]
    [InlineData("Unknown Item Alpha")]
    [InlineData("Mystery Artifact")]
    [InlineData("Phantom Relic")]
    public void ProcessItem_UnknownItem_LogsError(string itemName)
    {
        var logger = new FakeLogger();
        var processor = new ItemProcessor(logger, new GKItemRegistry(), new TrapManagerStub());

        processor.ProcessItem(CreateReceivedItem(itemName));

        Assert.Single(logger.ErrorLogs);
        Assert.Contains($"Could not process item {itemName}", logger.ErrorLogs[0]);
    }

    private static void AssertGrantApplied(ClientContractItem item, FakePlayerActions actions)
    {
        var grant = item.Grant;
        switch (grant.Type)
        {
            case "game_item":
                Assert.Equal("DropItem", actions.LastFunctionCalled);
                Assert.Equal(grant.ItemID, actions.LastDropItemId);
                break;
            case "craft_unlock":
                Assert.Equal("ApplyUnlocks", actions.LastFunctionCalled);
                Assert.Equal(
                    grant.CraftIDs.Select(id => (PlayerUtilities.UnlockType.Craft, id)),
                    actions.LastUnlocks
                );
                break;
            case "progressive_craft_unlock":
                Assert.Equal("ApplyUnlocks", actions.LastFunctionCalled);
                Assert.Equal(
                    grant.Stages[0].Select(id => (PlayerUtilities.UnlockType.Craft, id)),
                    actions.LastUnlocks
                );
                break;
            case "work_unlock":
                Assert.Equal("ApplyUnlocks", actions.LastFunctionCalled);
                Assert.Equal(
                    grant.WorkIDs.Select(id => (PlayerUtilities.UnlockType.Work, id))
                        .Concat(grant.PerkIDs.Select(id => (PlayerUtilities.UnlockType.Perk, id))),
                    actions.LastUnlocks
                );
                break;
            case "progressive_work_unlock":
                Assert.Equal("ApplyUnlocks", actions.LastFunctionCalled);
                Assert.Equal(
                    grant.Stages[0].Select(id => (PlayerUtilities.UnlockType.Work, id)),
                    actions.LastUnlocks
                );
                break;
            case "perk_unlock":
                Assert.Equal("ApplyUnlocks", actions.LastFunctionCalled);
                Assert.Equal(
                    new[] { (PlayerUtilities.UnlockType.Perk, grant.PerkID) }
                        .Concat(grant.CraftIDs.Select(id => (PlayerUtilities.UnlockType.Craft, id)))
                        .Concat(grant.WorkIDs.Select(id => (PlayerUtilities.UnlockType.Work, id))),
                    actions.LastUnlocks
                );
                break;
            case "permanent_buff":
                Assert.Equal("ApplyPermanentBuff", actions.LastFunctionCalled);
                Assert.Equal(grant.BuffID, actions.LastAppliedBuffId);
                break;
            case "relation":
                Assert.Equal("ApplyRelation", actions.LastFunctionCalled);
                Assert.Equal(grant.NpcID, actions.LastRelationNpcId);
                Assert.Equal(grant.Amount, actions.LastRelationAmount);
                break;
            case "research_points":
                Assert.Equal("AddResearchPoints", actions.LastFunctionCalled);
                Assert.Equal(grant.Color, actions.LastResearchPointColor);
                Assert.Equal(grant.Amount, actions.LastResearchPointAmount);
                break;
            case "technology_unlock":
                Assert.Equal("UnlockTechnologies", actions.LastFunctionCalled);
                Assert.Equal(grant.TechnologyIDs, actions.LastUnlockedTechnologies);
                break;
            case "flow_script":
                Assert.Equal("RunFlowScript", actions.LastFunctionCalled);
                Assert.Equal(grant.ScriptID, actions.LastFlowScriptId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported test grant type '{grant.Type}'");
        }
    }

    private static ReceivedItem CreateReceivedItem(string itemName)
    {
        return new ReceivedItem("dummy", itemName, "dummy", 0L, 0L, 0L, 0);
    }
}
