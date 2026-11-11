using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.ArchipelagoUtilities.Net.Interfaces;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public class LocationTriggerServiceTests
{
    [Fact]
    public void RegistryAcceptsEveryEmbeddedTriggerAndMatchesDirectRelationRewards()
    {
        var contract = ClientContractLoader.LoadDefault();
        var registry = new LocationTriggerRegistry(contract, contract.Locations.Values.Select(location => location.Name));
        var checker = new RecordingChecker();
        var service = new LocationTriggerService(checker, registry);

        Assert.True(service.HandleFlowNode("farmer_comes_to_pl_house", 10, "Flow_AddRelation"));
        Assert.Equal(new[]
        {
            contract.Locations["LOCATION_FARMER_REWARD_REACT_TO_SONS_SECRET_DRINKING"].Name,
        }, checker.Checked);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void EitherInquisitorAnswerChecksTheSameLocation(int answer)
    {
        var (service, checker, names) = ForEmbedded("LOCATION_INQUISITOR_REWARD_BURN_THE_WITCH_FIREWOOD");

        Assert.Equal(FlowRewardSuppression.None, service.HandleFlowChoice("npc_inquisitor", 736, answer));
        Assert.Equal(FlowRewardSuppression.None, service.HandleFlowChoice("npc_inquisitor", 308, 4));
        Assert.False(service.HandleFlowNode("npc_inquisitor", 308, "Flow_MultiAnswer"));
        Assert.Equal(FlowRewardSuppression.Relation, service.HandleFlowChoice("npc_inquisitor", 308, answer));
        Assert.Equal(names, checker.Checked);
    }

    [Fact]
    public void ClothoPotionRouteBatchesOnlyLocationsActiveInTheSlot()
    {
        var (service, checker, names) = ForEmbedded(
            "LOCATION_CLOTHO_REWARD_GIVE_CAULDRON",
            "LOCATION_CLOTHO_REWARD_GIVE_BEE");

        Assert.Equal(FlowRewardSuppression.None, service.HandleFlowChoice("npc_witch", 3, 2));
        Assert.Equal(FlowRewardSuppression.Relation, service.HandleFlowChoice("npc_witch", 3, 1));
        Assert.Equal(names.OrderBy(name => name), checker.Checked.OrderBy(name => name));
        Assert.Equal(1, checker.BatchCount);
    }

    [Fact]
    public void ChoiceCanReplaceItemAndRelationRewardsAcrossFlowCallbacks()
    {
        var (service, checker) = ForFlow(
            FlowTrigger("flow_quest_reward", 7, 2),
            FlowTrigger("flow_relation_reward", 7, 2));

        var suppression = service.HandleFlowChoice("quest", 7, 2);

        Assert.Equal(FlowRewardSuppression.Quest | FlowRewardSuppression.Relation, suppression);
        Assert.True(service.HandleFlowNode("callback", 1, "Flow_AddItemToWGO", suppression));
        Assert.True(service.HandleFlowNode("callback", 2, "Flow_AddWGOParam", suppression));
        Assert.False(service.HandleFlowNode("callback", 3, "Flow_UnlockPhrase", suppression));
        Assert.False(service.HandleFlowNode("callback", 2, "Flow_AddRelation"));
        Assert.Equal(new[] { "Reward" }, checker.Checked);
    }

    [Fact]
    public void DirectItemTriggerChecksLocationAndOnlyReplacesItemRewards()
    {
        var (service, checker) = ForFlow(
            FlowTrigger("flow_quest_reward", 12),
            FlowTrigger("flow_quest_reward", 13));

        Assert.False(service.HandleFlowNode(null!, 12, "Flow_DropItem", FlowRewardSuppression.Quest));
        Assert.False(service.HandleFlowNode("quest", 0, "Flow_DropItem", FlowRewardSuppression.Quest));
        Assert.False(service.HandleFlowNode("quest", 12, null!));
        Assert.False(service.HandleFlowNode("quest", 13, "Flow_AddRelation"));
        Assert.True(service.HandleFlowNode("quest", 12, "Flow_DropItem"));
        Assert.Equal(new[] { "Reward", "Reward" }, checker.Checked);
    }

    [Fact]
    public void CraftCompletionAndCraftUnlockUseSeparateTriggers()
    {
        const string flesh = "ex:mf_preparation_1:flesh";
        const string picture = "keeper_room_builddesk:p:keeper_room_picture_7";
        var (service, checker, names) = ForEmbedded(
            "LOCATION_TUTORIAL_REWARD_GET_FIRST_FLESH", "LOCATION_DONKEY_MEET");

        service.HandleCraftCompletion(picture);
        Assert.False(service.HandleCraftUnlock(flesh));
        Assert.False(service.ShouldSuppressCraftReveal(new[] { flesh }));
        Assert.True(service.ShouldSuppressCraftReveal(new[] { flesh, picture }));
        Assert.Empty(checker.Checked);

        service.HandleCraftCompletion(flesh);
        Assert.True(service.HandleCraftUnlock(picture));
        Assert.Equal(names, checker.Checked);
    }

    [Theory]
    [InlineData("Persistence")]
    [InlineData("Industriousness")]
    [InlineData("Eloquence")]
    public void EachGameOfCroneEndingChecksTheSameLocation(string technology)
    {
        var (service, checker, names) = ForEmbedded("LOCATION_GAME_OF_CRONE_REWARD_COMPLETE_STORY");

        Assert.True(service.HandleTechnology(technology));
        Assert.Equal(names, checker.Checked);
    }

    [Fact]
    public void ForcedTechnologyRewardDialogCanCheckTheLocation()
    {
        var (service, checker, names) = ForEmbedded("LOCATION_TUTORIAL_REWARD_ENTER_MORGUE_WITH_CORPSE");

        Assert.True(service.HandleTechnologyDialog("First slice", true, false, false));
        Assert.Equal(names, checker.Checked);
    }

    [Fact]
    public void ResearchAndRecipeBookReportTheirChecksInSeparateBatches()
    {
        var research = new[] { "Hardspares - Unlock 1", "Hardspares - Unlock 2", "Hardspares - Unlock 3" };
        var book = "Simple and Tasty - Unlock 1";
        var checker = new RecordingChecker();
        var service = ForResearch(checker, [.. research, book, "Simple and Tasty - Unlock 2"]);

        Assert.False(service.HandleTechnologyDialog("Hardspares", true, false, false));
        Assert.False(service.HandleTechnology("2min"));
        service.HandleTechnologyResearch("recipe_2min");
        Assert.Empty(checker.Checked);

        service.HandleTechnologyResearch("Hardspares");
        Assert.Equal(research, checker.Checked);
        service.HandleTechnologyResearch("2min");
        Assert.Equal([.. research, book], checker.Checked);
        Assert.Equal(2, checker.BatchCount);
    }

    [Theory]
    [InlineData("First slice", "LOCATION_TUTORIAL_REWARD_ENTER_MORGUE_WITH_CORPSE")]
    [InlineData("Grave plate", "LOCATION_TUTORIAL_REWARD_TALK_TO_BISHOP_1")]
    [InlineData("Iron", "LOCATION_KREZVOLD_REWARD_ASK_ABOUT_METAL_ORE")]
    public void QuestTechnologiesUseOnlyTheirExistingQuestCheck(string technology, string rewardKey)
    {
        var reward = ClientContractLoader.LoadDefault().Locations[rewardKey].Name;
        var checker = new RecordingChecker();
        var service = ForResearch(checker, [reward, $"{technology} - Unlock 1"]);

        service.HandleTechnologyResearch(technology);
        Assert.Empty(checker.Checked);
        Assert.True(service.HandleTechnologyDialog(technology, true, false, false));
        Assert.Equal([reward], checker.Checked);
    }

    [Fact]
    public void ResearchOnlyReportsLocationsInTheSlot()
    {
        const string active = "Hardspares - Unlock 2";
        var checker = new RecordingChecker();
        var service = ForResearch(checker, [active]);

        service.HandleTechnologyResearch("soul_sins_2");
        service.HandleTechnologyResearch("hardspares");
        service.HandleTechnologyResearch(null!);
        Assert.Empty(checker.Checked);

        service.HandleTechnologyResearch("Hardspares");
        Assert.Equal([active], checker.Checked);
        Assert.Equal(1, checker.BatchCount);
    }

    [Fact]
    public void RepeatedResearchReportsAreDeduplicatedByTheSameChecker()
    {
        const string book = "Simple and Tasty - Unlock 1";
        var logger = Mock.Of<ILogger>();
        var locations = new Mock<IArchipelagoLoader<ArchipelagoLocation>>();
        locations.Setup(loader => loader.LoadAll(It.IsAny<string[]>()))
            .Returns(new[] { new ArchipelagoLocation(book, 123) });
        var dataPackage = new DataPackageCache(Mock.Of<IArchipelagoLoader<ArchipelagoItem>>(), locations.Object,
            "graveyard_keeper");
        var client = new Mock<ArchipelagoClient>(logger, dataPackage);
        client.SetupGet(value => value.GameName).Returns("Graveyard Keeper");
        var checker = new ResearchLocationChecker(logger, client.Object);
        var service = ForResearch(checker, [book]);

        service.HandleTechnologyResearch("2min");
        service.HandleTechnologyResearch("2min");
        ForResearch(checker, [book]).HandleTechnologyResearch("2min");

        Assert.Equal([book], checker.GetAllLocationsAlreadyChecked());
    }

    [Theory]
    [InlineData("First slice", false, false, false)]
    [InlineData("First slice", true, true, false)]
    [InlineData("First slice", true, false, true)]
    [InlineData("Unmapped technology", true, false, false)]
    [InlineData(null, true, false, false)]
    public void OtherTechnologyDialogsKeepTheirVanillaBehavior(
        string? technology, bool forcedUnlock, bool revealTech, bool pseudotech)
    {
        var (service, checker, _) = ForEmbedded("LOCATION_TUTORIAL_REWARD_ENTER_MORGUE_WITH_CORPSE");

        Assert.False(service.HandleTechnologyDialog(technology!, forcedUnlock, revealTech, pseudotech));
        Assert.Empty(checker.Checked);
    }

    [Fact]
    public void WorldItemTriggerRequiresAnExactActiveMatch()
    {
        var (service, checker, names) = ForEmbedded("LOCATION_VILLAGE_REWARD_DIG_UP_DOOR_HINGE");

        Assert.False(service.HandleWorldItem(null!));
        Assert.False(service.HandleWorldItem("TR_DOOR_HINGE"));
        Assert.False(service.HandleWorldItem("tr_helmet"));
        Assert.True(service.HandleWorldItem("tr_door_hinge"));
        Assert.Equal(names, checker.Checked);
    }

    [Fact]
    public void EmptySlotLocationSetNeverReplacesVanillaRewards()
    {
        var (service, checker, _) = ForEmbedded();

        Assert.Equal(FlowRewardSuppression.None, service.HandleFlowChoice("npc_witch", 3, 1));
        Assert.False(service.HandleFlowNode("ghost_comes_after_players_sleep", 24, "Flow_DropItem"));
        service.HandleCraftCompletion("ex:mf_preparation_1:flesh");
        Assert.False(service.HandleCraftUnlock("keeper_room_builddesk:p:keeper_room_picture_7"));
        Assert.False(service.HandleTechnology("Persistence"));
        Assert.False(service.HandleWorldItem("tr_door_hinge"));
        Assert.Empty(checker.Checked);
    }

    private static (LocationTriggerService Service, RecordingChecker Checker, string[] Names)
        ForEmbedded(params string[] locationKeys)
    {
        var contract = ClientContractLoader.LoadDefault();
        var names = locationKeys.Select(key => contract.Locations[key].Name).ToArray();
        var checker = new RecordingChecker();
        return (new LocationTriggerService(checker, new LocationTriggerRegistry(contract, names)), checker, names);
    }

    private static LocationTriggerService ForResearch(ILocationChecker checker, string[] names)
        => new(checker, new LocationTriggerRegistry(ClientContractLoader.LoadDefault(), names,
            TechnologyCatalog.LoadDefault()));

    private static (LocationTriggerService Service, RecordingChecker Checker)
        ForFlow(params ClientContractLocationTrigger[] triggers)
    {
        var contract = new ClientContract
        {
            Locations = new Dictionary<string, ClientContractLocation>
            {
                ["LOCATION_TEST"] = new() { Name = "Reward", Triggers = triggers.ToList() },
            },
        };
        var checker = new RecordingChecker();
        return (new LocationTriggerService(checker, new LocationTriggerRegistry(contract, new[] { "Reward" })),
            checker);
    }

    private static ClientContractLocationTrigger FlowTrigger(string type, int nodeID, int? answer = null)
        => new() { Type = type, Graph = "quest", RuntimeNodeID = nodeID, AnswerIndex = answer };

    private sealed class ResearchLocationChecker(ILogger logger, ArchipelagoClient client)
        : LocationChecker(logger, client, []), ILocationChecker;

    private sealed class RecordingChecker : ILocationChecker
    {
        public List<string> Checked { get; } = new();
        public int BatchCount { get; private set; }

        public void AddCheckedLocations(string[] locationNames)
        {
            BatchCount++;
            Checked.AddRange(locationNames);
        }

        public void AddCheckedLocation(string locationName) => AddCheckedLocations(new[] { locationName });
    }
}
