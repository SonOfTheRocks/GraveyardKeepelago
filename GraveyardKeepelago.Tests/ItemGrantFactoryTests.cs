using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Logic;
using KaitoKid.Utilities.Interfaces;
using Moq;
using static GraveyardKeepelago.GameModifications.PlayerUtilities.UnlockType;

namespace GraveyardKeepelago.Tests;

public sealed class ItemGrantFactoryTests
{
    [Fact]
    public void EveryEmbeddedItemCreatesItsExactGrantAndPayload()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var factory = new ItemGrantFactory(items, Mock.Of<ILogger>());

        foreach (var item in items)
        {
            var definition = item.Grant;
            if (definition.Stages != null)
            {
                var type = definition.Type == "progressive_craft_unlock" ? Craft : Work;
                for (var stage = 0; stage < definition.Stages.Count; stage++)
                    Assert.Equal(definition.Stages[stage].Select(id => (type, id)),
                        Assert.IsType<UnlockGrant>(factory.Create(item.Name, stage)).Unlocks);

                Assert.Equal(GrantResult.Applied, factory.Create(item.Name, definition.Stages.Count).Apply(null!));
                continue;
            }

            var grant = factory.Create(item.Name, 0);
            switch (definition.Type)
            {
                case "trap":
                    Assert.Equal(definition.TrapID, Assert.IsType<TrapGrant>(grant).TrapID);
                    break;
                case "game_item":
                    Assert.Equal(definition.ItemID, Assert.IsType<PhysicalItemGrant>(grant).ItemID);
                    break;
                case "craft_unlock":
                    Assert.Equal(definition.CraftIDs.Select(id => (Craft, id)), Assert.IsType<UnlockGrant>(grant).Unlocks);
                    break;
                case "work_unlock":
                    Assert.Equal(definition.WorkIDs.Select(id => (Work, id))
                        .Concat(definition.PerkIDs.Select(id => (Perk, id))), Assert.IsType<UnlockGrant>(grant).Unlocks);
                    break;
                case "perk_unlock":
                    Assert.Equal(new[] { (Perk, definition.PerkID) }
                        .Concat(definition.CraftIDs.Select(id => (Craft, id)))
                        .Concat(definition.WorkIDs.Select(id => (Work, id))), Assert.IsType<UnlockGrant>(grant).Unlocks);
                    break;
                case "permanent_buff":
                    Assert.Equal(definition.BuffID, Assert.IsType<PermanentBuffGrant>(grant).BuffID);
                    break;
                case "relation":
                    var relation = Assert.IsType<RelationGrant>(grant);
                    Assert.Equal((definition.NpcID, definition.Amount), (relation.NpcID, relation.Amount));
                    break;
                case "research_points":
                    var research = Assert.IsType<ResearchPointsGrant>(grant);
                    Assert.Equal((definition.Color, definition.Amount), (research.Color, research.Amount));
                    break;
                case "technology_unlock":
                    Assert.Equal(definition.TechnologyIDs.AsEnumerable(), Assert.IsType<TechnologyUnlockGrant>(grant).TechnologyIDs);
                    break;
                case "flow_script":
                    Assert.Equal(definition.ScriptID, Assert.IsType<FlowScriptGrant>(grant).ScriptID);
                    break;
                default:
                    throw new InvalidDataException($"Unhandled grant '{definition.Type}' in the v9 data.");
            }
        }
    }

    [Fact]
    public void DisabledAndUnknownNamesProduceNoGrant()
    {
        var items = ClientContractLoader.LoadDefault().Items.Values;
        var logger = new Mock<ILogger>();
        var dlcs = new DLCManager(logger.Object, []);
        var factory = new ItemGrantFactory(items.Where(item => dlcs.IsContentEnabled(item.DLC)), logger.Object);

        var disabled = items.First(item => item.DLC != "BASE_GAME");
        Assert.Null(factory.Create(disabled.Name, 0));
        Assert.Null(factory.Create("bats Trap", 0));
        logger.Verify(log => log.LogError(It.Is<string>(message => message.Contains(disabled.Name))), Times.Once);
    }

    [Fact]
    public void AnUnconfirmedAttemptDoesNotAdvanceTheProgressiveStage()
    {
        var item = ClientContractLoader.LoadDefault().Items["ITEM_LEARN_GATHERING_PROGRESSIVE_TREE_FELLING"];
        var factory = new ItemGrantFactory([item], Mock.Of<ILogger>());

        Assert.Equal((Work, "t_wood_small"), Assert.Single(Assert.IsType<UnlockGrant>(factory.Create(item.Name, 0)).Unlocks));
        Assert.Equal((Work, "t_wood_big"), Assert.Single(Assert.IsType<UnlockGrant>(factory.Create(item.Name, 1)).Unlocks));
        Assert.Equal((Work, "t_wood_big"), Assert.Single(Assert.IsType<UnlockGrant>(factory.Create(item.Name, 1)).Unlocks));
    }
}
