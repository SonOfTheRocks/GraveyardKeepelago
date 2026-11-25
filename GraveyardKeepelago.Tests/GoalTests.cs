using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Goals;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public class GoalTests
{
    private static SlotData Slot(Dictionary<string, object> fields) => new("Keeper", fields, Mock.Of<ILogger>());

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(2484)]
    public void GraveyardUsesExactTransmittedInteger(int target)
    {
        var slot = Slot(new() { ["goal"] = 1L, ["graveyard_goal_rating"] = (long)target });
        Assert.Null(slot.GetGoalRejection());
        Assert.Equal(Goal.Graveyard, slot.Goal);
        Assert.Equal(target, slot.GraveyardGoalRating);
        Assert.False(GoalManager.IsComplete(slot.Goal, target, null!, target - 0.5f));
        Assert.True(GoalManager.IsComplete(slot.Goal, target, null!, target));
        Assert.True(GoalManager.IsComplete(slot.Goal, target, null!, target + 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(2485)]
    [InlineData(long.MaxValue)]
    [InlineData("500")]
    [InlineData(500.0)]
    [InlineData(true)]
    public void InvalidGraveyardTargetIsRejected(object? value)
    {
        Assert.NotNull(Slot(new() { ["goal"] = 1, ["graveyard_goal_rating"] = value! }).GetGoalRejection());
    }

    [Fact]
    public void MissingFieldsAreRejectedWithoutPortalFallback()
    {
        Assert.NotNull(Slot(new()).GetGoalRejection());
        Assert.NotNull(Slot(new() { ["goal"] = 1 }).GetGoalRejection());
        Assert.Null(Slot(new() { ["goal"] = 0 }).GetGoalRejection());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(4294967296L)]
    [InlineData("Portal")]
    [InlineData("0")]
    [InlineData(0.0)]
    [InlineData(false)]
    public void UnsupportedGoalIsRejected(object? value)
        => Assert.NotNull(Slot(new() { ["goal"] = value! }).GetGoalRejection());

    [Theory]
    [InlineData(null)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(-29f)]
    public void ZeroTargetNeedsRealNonnegativeRating(float? rating)
        => Assert.False(GoalManager.IsComplete(Goal.Graveyard, 0, null!, rating));

    [Fact]
    public void PortalNeedsBothCompletedCraftsAndIgnoresRating()
    {
        var crafts = new List<string>();
        Assert.False(GoalManager.IsComplete(Goal.Portal, 0, crafts, 2484));
        crafts.Add("craft_barrel");
        Assert.False(GoalManager.IsComplete(Goal.Portal, 0, crafts, null));
        crafts.Clear();
        crafts.Add("craft_emitter");
        Assert.False(GoalManager.IsComplete(Goal.Portal, 0, crafts, null));
        crafts.Add("craft_barrel");
        Assert.True(GoalManager.IsComplete(Goal.Portal, 0, crafts, null));
        Assert.False(GoalManager.IsComplete((Goal)2, 0, crafts, 2484));
    }

    [Fact]
    public void LoadedCompletionWaitsForReadyBindingAndReportsOnce()
    {
        var manager = new GoalManager();
        var sent = 0;
        Task Send() { sent++; return Task.CompletedTask; }
        manager.Update(0, () => false, () => throw new Exception("Must not read game"), Send, _ => { });
        Assert.Equal(0, sent);
        manager.Update(1, () => true, () => true, Send, _ => { });
        manager.Update(2, () => true, () => true, Send, _ => { });
        manager.Update(100, () => true, () => true, Send, _ => { });
        Assert.Equal(1, sent);
        // A new bound save or reconnected session gets a fresh manager.
        new GoalManager().Update(100, () => true, () => true, Send, _ => { });
        Assert.Equal(2, sent);
    }

    [Fact]
    public void BindingLostDuringReadPreventsTransmission()
    {
        var ready = true;
        new GoalManager().Update(0, () => ready, () => { ready = false; return true; },
            () => throw new Exception("Must not send"), _ => { });
    }

    [Fact]
    public void IncompleteGoalIsReevaluatedAndInFlightSendIsNotDuplicated()
    {
        var manager = new GoalManager();
        var sent = 0;
        var completion = new TaskCompletionSource();
        Task Send() { sent++; return completion.Task; }
        manager.Update(0, () => true, () => false, Send, _ => { });
        manager.Update(1, () => true, () => true, Send, _ => { });
        manager.Update(100, () => true, () => true, Send, _ => { });
        Assert.Equal(1, sent);
        completion.SetResult();
        manager.Update(101, () => true, () => true, Send, _ => { });
        manager.Update(200, () => true, () => true, Send, _ => { });
        Assert.Equal(1, sent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransmissionFailuresRetryAfterDelay(bool synchronous)
    {
        var manager = new GoalManager();
        var sent = 0;
        var errors = new List<string>();
        Task Send()
        {
            sent++;
            if (sent > 1) return Task.CompletedTask;
            if (synchronous) throw new IOException("offline");
            return Task.FromException(new IOException("offline"));
        }
        manager.Update(0, () => true, () => true, Send, errors.Add);
        manager.Update(1, () => true, () => true, Send, errors.Add);
        manager.Update(4, () => true, () => true, Send, errors.Add);
        Assert.Equal(1, sent);
        manager.Update(6, () => false, () => true, Send, errors.Add);
        Assert.Equal(1, sent);
        manager.Update(7, () => true, () => true, Send, errors.Add);
        Assert.Equal(2, sent);
        Assert.Single(errors);
        manager.Update(8, () => true, () => true, Send, errors.Add);
        manager.Update(100, () => true, () => true, Send, errors.Add);
        Assert.Equal(2, sent);
    }

    [Fact]
    public void CancelledTransmissionAndUnavailableNativeStateRemainRetryable()
    {
        var manager = new GoalManager();
        var errors = new List<string>();
        var sent = 0;
        Task Send() { sent++; return Task.FromCanceled(new CancellationToken(true)); }
        manager.Update(0, () => true, () => throw new InvalidOperationException("zone not ready"), Send, errors.Add);
        manager.Update(4, () => true, () => true, Send, errors.Add);
        Assert.Equal(0, sent);
        manager.Update(5, () => true, () => true, Send, errors.Add);
        manager.Update(6, () => true, () => true, Send, errors.Add);
        manager.Update(11, () => true, () => true, () => { sent++; return Task.CompletedTask; }, errors.Add);
        Assert.Equal(2, sent);
        Assert.Equal(2, errors.Count);
    }
}
