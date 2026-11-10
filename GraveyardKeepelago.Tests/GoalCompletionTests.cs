using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Goals;

namespace GraveyardKeepelago.Tests;

public class GoalCompletionTests
{
    private static readonly string[] PortalCrafts = { "craft_barrel", "craft_emitter" };

    [Theory]
    [InlineData(499.9f, false)]
    [InlineData(500f, true)]
    [InlineData(500.5f, true)]
    public void GraveyardUsesTheActualFractionalRating(float rating, bool expected)
    {
        var reports = 0;
        var goal = new GoalCompletionTracker(Goal.Graveyard, 500);
        Assert.Equal(expected, goal.Check(true, true, null!, rating, () => reports++));
        Assert.Equal(expected ? 1 : 0, reports);
    }

    [Fact]
    public void GraveyardRemainsPendingUntilItsRatingBecomesAvailable()
    {
        var goal = new GoalCompletionTracker(Goal.Graveyard, 500);
        var reports = 0;
        Assert.False(goal.Check(true, true, null!, null, () => reports++));
        Assert.True(goal.Check(true, true, null!, 500f, () => reports++));
        Assert.Equal(1, reports);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoGoalReportsWithoutAnInitializedWorldAndSave(bool zeroThreshold)
    {
        var goal = new GoalCompletionTracker(Goal.Graveyard, zeroThreshold ? 0 : 500);
        Assert.False(goal.Check(true, false, PortalCrafts, 1000f,
            () => Assert.Fail("Completion was sent while the world was loading")));
        Assert.False(new GoalCompletionTracker(Goal.Portal, null).Check(true, false, PortalCrafts, null,
            () => Assert.Fail("Portal completion was sent without a ready save")));
    }

    [Theory]
    [InlineData(-29f)]
    [InlineData(0f)]
    [InlineData(null)]
    public void ZeroThresholdMatchesTheApworldImmediateCompletionRule(float? rating)
    {
        var reports = 0;
        Assert.True(new GoalCompletionTracker(Goal.Graveyard, 0).Check(true, true, null!, rating, () => reports++));
        Assert.Equal(1, reports);
    }

    [Fact]
    public void PortalNeedsBothPersistedCrafts()
    {
        var goal = new GoalCompletionTracker(Goal.Portal, null);
        foreach (var crafts in new[] { null, Array.Empty<string>(), new[] { "craft_barrel" }, new[] { "craft_emitter" } })
            Assert.False(goal.Check(true, true, crafts!, null, () => Assert.Fail("Portal was incomplete")));
        Assert.True(goal.Check(true, true, PortalCrafts, null, () => { }));
    }

    [Theory]
    [InlineData(Goal.Portal)]
    [InlineData(Goal.Graveyard)]
    public void OfflineCompletionIsSentAfterReconnectAndAgainAfterSaveReload(Goal type)
    {
        var goal = new GoalCompletionTracker(type, type == Goal.Graveyard ? 500 : null);
        var reports = 0;
        Assert.False(goal.Check(false, true, PortalCrafts, 500f, () => reports++));
        Assert.True(goal.Check(true, true, PortalCrafts, 500f, () => reports++));
        for (var frame = 0; frame < 10; frame++)
            Assert.False(goal.Check(true, true, PortalCrafts, 500f, () => reports++));
        Assert.Equal(1, reports);
        goal.Check(false, true, PortalCrafts, 500f, () => reports++);
        Assert.True(goal.Check(true, true, PortalCrafts, 500f, () => reports++));
        var reloaded = new GoalCompletionTracker(type, type == Goal.Graveyard ? 500 : null);
        Assert.True(reloaded.Check(true, true, PortalCrafts, 500f, () => reports++));
        Assert.Equal(3, reports);
    }

    [Fact]
    public void ExplicitReconnectRetriesEvenWhenTheOfflineFrameWasNotObserved()
    {
        var goal = new GoalCompletionTracker(Goal.Portal, null);
        var reports = 0;
        goal.Check(true, true, PortalCrafts, null, () => reports++);
        goal.ConnectionChanged();
        Assert.True(goal.Check(true, true, PortalCrafts, null, () => reports++));
        Assert.Equal(2, reports);
    }

    [Fact]
    public void FailedSendIsRetryableAndDoesNotAcknowledgeCompletion()
    {
        var goal = new GoalCompletionTracker(Goal.Portal, null);
        Assert.Throws<IOException>(() => goal.Check(true, true, PortalCrafts, null, () => throw new IOException("Disconnected")));
        Assert.True(goal.Check(true, true, PortalCrafts, null, () => { }));
    }

    [Fact]
    public void SwitchingGoalsUsesTheNewSavesOwnCompletionCondition()
    {
        var graveyard = new GoalCompletionTracker(Goal.Graveyard, 500);
        graveyard.Check(true, true, PortalCrafts, 500, () => { });
        var portal = new GoalCompletionTracker(Goal.Portal, null);
        Assert.False(portal.Check(true, true, Array.Empty<string>(), 500, () => Assert.Fail("Inherited another save's completion")));
        Assert.True(portal.Check(true, true, PortalCrafts, null, () => { }));
    }

    [Fact]
    public void UnsupportedGoalsOrThresholdsCannotCreateATracker()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GoalCompletionTracker((Goal)2, null));
        foreach (var rating in new int?[] { null, -1, 2485 })
            Assert.Throws<ArgumentOutOfRangeException>(() => new GoalCompletionTracker(Goal.Graveyard, rating));
    }
}
