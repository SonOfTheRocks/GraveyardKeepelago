using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Progression;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class RelationRewardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbsentNpcOrBrokenBubbleDoesNotStopItemsOrRepeatHappinessOnReplay(bool brokenBubble)
    {
        var progress = new SaveProgress();
        var ledger = new ItemReceiptLedger(progress, () => { });
        var known = new HashSet<string>();
        var happiness = 0;
        var laterItems = 0;
        var presentationErrors = new List<Exception>();
        var history = new[]
        {
            new ReceivedItem("Check", "Gerry - Happiness +10", "Player1", 100, 200, 1, 0),
            new ReceivedItem("Next", "Another reward", "Player1", 101, 201, 1, 1),
        };
        bool Apply(ReceivedItem receipt)
        {
            if (receipt.UniqueId == 0)
                RelationReward.Apply("crafting_skull_3", 10, id => known.Add(id),
                    (_, amount) => happiness += amount,
                    brokenBubble ? (_, _) => throw new NullReferenceException("Absent world object") : null!,
                    presentationErrors.Add);
            else laterItems++;
            return true;
        }
        ledger.ApplyHistory(history, Apply);
        ledger.ApplyHistory(history, Apply);
        Assert.Contains("crafting_skull_3", known);
        Assert.Equal(10, happiness);
        Assert.Equal(1, laterItems);
        Assert.Equal(2, progress.Receipts.Count);
        Assert.Equal(brokenBubble ? 1 : 0, presentationErrors.Count);
    }

    [Fact]
    public void KnownNpcFailureDoesNotPartiallyIncreaseHappiness()
    {
        var happiness = 0;
        Assert.Throws<InvalidOperationException>(() => RelationReward.Apply("npc", 10,
            _ => throw new InvalidOperationException("Save not ready"),
            (_, amount) => happiness += amount, null!, _ => Assert.Fail("Required failure swallowed")));
        Assert.Equal(0, happiness);
    }

    [Fact]
    public void RequiredRelationUpdateFailureIsStillReported()
    {
        Assert.Throws<InvalidOperationException>(() => RelationReward.Apply("npc", 10,
            _ => { }, (_, _) => throw new InvalidOperationException("Cannot update save"),
            (_, _) => Assert.Fail("Bubble shown before reward succeeded"), _ => { }));
    }
}
