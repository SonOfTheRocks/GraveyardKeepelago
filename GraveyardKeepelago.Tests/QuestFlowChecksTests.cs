using FlowCanvas;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using Moq;

namespace GraveyardKeepelago.Tests;

public class QuestFlowChecksTests
{
    [Fact]
    public void CallbackFallsBackToVanillaAfterItsSaveStopsBeingCurrent()
    {
        var checks = new QuestFlowChecks();
        var (service, checker) = CreateSlot("Reward", Trigger("flow_quest_reward", 12));
        var currentSave = true;
        checks.StartSlot(service, () => currentSave);
        var vanillaCalls = 0;
        var continuations = 0;
        FlowHandler vanilla = _ => vanillaCalls++;
        var output = checks.Wrap(vanilla, vanilla, _ => continuations++, "quest", 1, null, 12, "Flow_DropItem");

        output(Flow.New);
        currentSave = false;
        output(Flow.New);

        Assert.Equal(1, continuations);
        Assert.Equal(1, vanillaCalls);
        checker.Verify(c => c.AddCheckedLocations(new[] { "Reward" }), Times.Once);
    }

    [Fact]
    public void BindingDoesNotStackAndCanBeRestoredAfterVanillaRebinds()
    {
        var checks = new QuestFlowChecks();
        var (service, checker) = CreateSlot("Reward", Trigger("flow_quest_reward", 12));
        checks.StartSlot(service);
        FlowHandler vanilla = _ => throw new InvalidOperationException("Expected the reward to be replaced.");
        var continuations = 0;
        FlowHandler next = _ => continuations++;

        var bound = checks.Wrap(vanilla, vanilla, next, "quest", 1, null, 12, "Flow_DropItem");
        var boundAgain = checks.Wrap(bound, vanilla, next, "quest", 1, null, 12, "Flow_DropItem");
        Assert.Same(bound, boundAgain);
        boundAgain(Flow.New);
        var rebound = checks.Wrap(vanilla, vanilla, next, "quest", 1, null, 12, "Flow_DropItem");
        rebound(Flow.New);

        Assert.Equal(2, continuations);
        checker.Verify(c => c.AddCheckedLocations(new[] { "Reward" }), Times.Exactly(2));
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void VanillaRewardRunsWhenAnyReplacementConditionIsMissing(
        bool hasSlot, bool locationActive, bool canContinue)
    {
        var checks = new QuestFlowChecks();
        var (service, checker) = CreateSlot("Reward", Trigger("flow_quest_reward", 12), locationActive);
        if (hasSlot)
            checks.StartSlot(service);
        var vanillaCalls = 0;
        FlowHandler vanilla = _ => vanillaCalls++;
        FlowHandler? continuation = canContinue
            ? _ => throw new InvalidOperationException("Expected the vanilla node to run.")
            : null;

        var output = checks.Wrap(vanilla, vanilla, continuation, "quest", 1, null, 12, "Flow_DropItem");
        output(Flow.New);

        Assert.Equal(1, vanillaCalls);
        checker.VerifyNoOtherCalls();
    }

    [Fact]
    public void ChoiceSuppressionFollowsItsRouteAndOnlyAffectsTheMatchingRewardType()
    {
        var checks = new QuestFlowChecks();
        var (service, checker) = CreateSlot("Relation reward", Trigger("flow_relation_reward", 7, 2));
        checks.StartSlot(service);
        var route = ChooseAnswer(checks, 2);
        var vanillaCalls = 0;
        var continuations = 0;
        FlowHandler vanilla = _ => vanillaCalls++;
        var itemOutput = checks.Wrap(vanilla, vanilla, _ => continuations++, "callback", 1, null, 12, "Flow_DropItem");
        var relationOutput = checks.Wrap(vanilla, vanilla, _ => continuations++, "callback", 1, null, 12, "Flow_AddRelation");

        itemOutput(route);
        relationOutput(route);
        relationOutput(ChooseAnswer(checks, 3));

        Assert.Equal(2, vanillaCalls);
        Assert.Equal(1, continuations);
        checker.Verify(c => c.AddCheckedLocations(new[] { "Relation reward" }), Times.Once);
        checker.VerifyNoOtherCalls();
    }

    [Fact]
    public void BoundOutputsUseTheNewSlotAndIgnoreCallbacksFromThePreviousSlot()
    {
        var checks = new QuestFlowChecks();
        var (first, firstChecker) = CreateSlot("First", Trigger("flow_relation_reward", 7, 2));
        var (second, secondChecker) = CreateSlot("Second", Trigger("flow_relation_reward", 12));
        checks.StartSlot(first);
        var oldRoute = ChooseAnswer(checks, 2);
        var vanillaCalls = 0;
        var continuations = 0;
        FlowHandler vanilla = _ => vanillaCalls++;
        var output = checks.Wrap(vanilla, vanilla, _ => continuations++, "quest", 1, null, 12, "Flow_AddRelation");

        checks.Clear();
        output(oldRoute);
        checks.StartSlot(second);
        output(oldRoute);
        output(Flow.New);

        Assert.Equal(2, vanillaCalls);
        Assert.Equal(1, continuations);
        firstChecker.Verify(c => c.AddCheckedLocations(new[] { "First" }), Times.Once);
        secondChecker.Verify(c => c.AddCheckedLocations(new[] { "Second" }), Times.Once);
        firstChecker.VerifyNoOtherCalls();
        secondChecker.VerifyNoOtherCalls();
    }

    private static Flow ChooseAnswer(QuestFlowChecks checks, int answer)
    {
        var result = Flow.New;
        FlowHandler vanilla = flow => result = flow;
        var output = checks.Wrap(vanilla, vanilla, null, "quest", 7, answer, 8, "Flow_ShowPhrase");
        output(Flow.New);
        return result;
    }

    private static ClientContractLocationTrigger Trigger(string type, int nodeID, int? answer = null)
        => new() { Type = type, Graph = "quest", RuntimeNodeID = nodeID, AnswerIndex = answer };

    private static (LocationTriggerService, Mock<ILocationChecker>) CreateSlot(
        string name, ClientContractLocationTrigger trigger, bool active = true)
    {
        var checker = new Mock<ILocationChecker>();
        var contract = new ClientContract
        {
            Locations = new Dictionary<string, ClientContractLocation>
            {
                ["LOCATION_TEST"] = new() { Name = name, Triggers = [trigger] },
            },
        };
        var registry = new LocationTriggerRegistry(contract, active ? [name] : []);
        return (new LocationTriggerService(checker.Object, registry), checker);
    }
}
