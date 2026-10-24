using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public class LocationTriggerServiceTests
{
    [Fact]
    public void Registry_IndexesAllSharedTriggerKinds()
    {
        var registry = new LocationTriggerRegistry(ClientContractLoader.LoadDefault());

        Assert.NotEmpty(registry.GetFlow("npc_astrologer", 19, 1));
        Assert.NotEmpty(registry.GetCraft("ex:mf_preparation_1:flesh"));
        Assert.NotEmpty(registry.GetTechnology("Persistence"));
        Assert.NotEmpty(registry.GetWorldItem("tr_door_hinge"));
    }

    [Fact]
    public void DirectFlowReward_ChecksLocationAndSuppressesOnlyTheRewardNode()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Reward", LocationTriggerTypes.FlowQuestReward, graph: "quest", nodeID: "12"));

        Assert.True(service.HandleFlowNode("quest", 12, "Flow_DropItem"));
        Assert.False(service.HandleFlowNode("quest", 13, "Flow_UnlockPhrase"));
        Assert.Equal(new[] { "Reward" }, checker.Checked);
    }

    [Fact]
    public void UnidentifiedFlowNode_IsNeverSuppressed()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Reward", LocationTriggerTypes.FlowQuestReward, graph: "quest", nodeID: "12"));

        Assert.False(service.HandleFlowNode(null, 12, "Flow_DropItem"));
        Assert.False(service.HandleFlowNode("quest", -1, "Flow_DropItem"));
        Assert.False(service.HandleFlowNode("quest", 12, null));
        Assert.Equal(FlowRewardSuppression.None, service.HandleFlowChoice(null, 7, 1));
        Assert.Empty(checker.Checked);
    }

    [Fact]
    public void FlowChoice_SuppressionCanFollowAsynchronousFlowCallbacks()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Relation reward", LocationTriggerTypes.FlowRelationReward,
                graph: "quest", nodeID: "7", answerIndex: 2));

        var suppression = service.HandleFlowChoice("quest", 7, 2);

        Assert.True(service.HandleFlowNode("other", 99, "Flow_AddRelation", suppression));
        Assert.False(service.HandleFlowNode("other", 99, "Flow_DropItem", suppression));
        Assert.False(service.HandleFlowNode("other", 99, "Flow_AddRelation"));
        Assert.Equal(new[] { "Relation reward" }, checker.Checked);
    }

    [Fact]
    public void OneChoice_CanCheckSeveralEquivalentRewards()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("First", LocationTriggerTypes.FlowRelationReward, "quest", "7", 2),
            Trigger("Second", LocationTriggerTypes.FlowRelationReward, "quest", "7", 2),
            Trigger("Third", LocationTriggerTypes.FlowRelationReward, "quest", "7", 2));

        var suppression = service.HandleFlowChoice("quest", 7, 2);

        Assert.Equal(FlowRewardSuppression.Relation, suppression);
        Assert.Equal(new[] { "First", "Second", "Third" }, checker.Checked);
    }

    [Fact]
    public void AlternativeTechnologyTriggers_CheckTheSameLocation()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Finish Game of Crone", LocationTriggerTypes.TechnologyReward, technologyID: "Persistence"),
            Trigger("Finish Game of Crone", LocationTriggerTypes.TechnologyReward, technologyID: "Eloquence"));

        Assert.True(service.HandleTechnology("Persistence"));
        Assert.True(service.HandleTechnology("Eloquence"));
        Assert.Equal(new[] { "Finish Game of Crone", "Finish Game of Crone" }, checker.Checked);
    }

    [Fact]
    public void ChoiceWithItemAndRelationRewards_SuppressesBothKinds()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Item", LocationTriggerTypes.FlowQuestReward, "quest", "7", 2),
            Trigger("Relation", LocationTriggerTypes.FlowRelationReward, "quest", "7", 2));

        var suppression = service.HandleFlowChoice("quest", 7, 2);

        Assert.Equal(FlowRewardSuppression.Quest | FlowRewardSuppression.Relation, suppression);
        Assert.True(service.HandleFlowNode("other", 1, "Flow_DropItem", suppression));
        Assert.True(service.HandleFlowNode("other", 2, "Flow_AddRelation", suppression));
    }

    [Fact]
    public void CraftAndWorldItemTriggers_CheckOnlyMappedIdentifiers()
    {
        var checker = new RecordingLocationChecker();
        var service = BuildService(checker,
            Trigger("Craft", LocationTriggerTypes.CraftReward, craftID: "craft"),
            Trigger("World item", LocationTriggerTypes.WorldItemReward, itemID: "item"));

        service.HandleCraft("other");
        Assert.False(service.HandleWorldItem("other"));
        service.HandleCraft("craft");
        Assert.True(service.HandleWorldItem("item"));

        Assert.Equal(new[] { "Craft", "World item" }, checker.Checked);
    }

    private static LocationTriggerService BuildService(RecordingLocationChecker checker,
        params TriggerDefinition[] definitions)
    {
        var locations = new Dictionary<string, ClientContractLocation>();
        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            var key = definition.LocationName + index;
            if (!locations.TryGetValue(key, out var location))
            {
                location = new ClientContractLocation
                {
                    Name = definition.LocationName,
                    ParentRegion = "Test",
                    Triggers = new List<ClientContractLocationTrigger>(),
                };
                locations.Add(key, location);
            }
            location.Triggers.Add(definition.Trigger);
        }

        return new LocationTriggerService(checker,
            new LocationTriggerRegistry(new ClientContract { Locations = locations }));
    }

    private static TriggerDefinition Trigger(string locationName, string type, string? graph = null,
        string? nodeID = null, int? answerIndex = null, string? craftID = null, string? itemID = null,
        string? technologyID = null)
        => new TriggerDefinition(locationName, new ClientContractLocationTrigger
        {
            Type = type,
            Graph = graph,
            NodeID = nodeID,
            RuntimeNodeID = nodeID == null ? 0 : int.Parse(nodeID),
            AnswerIndex = answerIndex,
            CraftID = craftID,
            ItemID = itemID,
            TechnologyID = technologyID,
        });

    private sealed class TriggerDefinition
    {
        public TriggerDefinition(string locationName, ClientContractLocationTrigger trigger)
        {
            LocationName = locationName;
            Trigger = trigger;
        }

        public string LocationName { get; }
        public ClientContractLocationTrigger Trigger { get; }
    }

    private sealed class RecordingLocationChecker : ILocationChecker
    {
        public List<string> Checked { get; } = new List<string>();

        public void AddCheckedLocations(string[] locationNames) => Checked.AddRange(locationNames);
        public void AddCheckedLocation(string locationName) => Checked.Add(locationName);
    }
}
