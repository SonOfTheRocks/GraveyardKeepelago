using System.IO;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class GoalSlotDataTests
{
    [Fact]
    public void PortalSlotsWithoutAGraveyardThresholdRemainCompatible()
        => Assert.Equal(Goal.Portal, Read(new() { ["goal"] = 0L }).Goal);

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(2484)]
    public void GraveyardKeepsTheAuthoritativeThreshold(int rating)
    {
        foreach (var value in new object[] { rating, (long)rating })
        {
            var slot = Read(new() { ["goal"] = 1L, ["graveyard_goal_rating"] = value });
            Assert.Equal(Goal.Graveyard, slot.Goal);
            Assert.Equal(rating, slot.GraveyardGoalRating);
        }
    }

    [Fact]
    public void MissingGoalRemainsCompatibleWithOldPortalSlots()
    {
        var slot = Read(new());
        Assert.Equal(Goal.Portal, slot.Goal);
        Assert.Null(slot.GraveyardGoalRating);
    }

    [Theory]
    [InlineData(2L)]
    [InlineData(-1L)]
    [InlineData("invalid")]
    [InlineData(true)]
    [InlineData(null)]
    public void UnsupportedGoalsAreRejectedRatherThanTreatedAsPortal(object? value)
        => Assert.Throws<InvalidDataException>(() => Read(new() { ["goal"] = value! }));

    [Fact]
    public void GraveyardSlotsMustSupplyTheirActualThreshold()
        => Assert.Throws<InvalidDataException>(() => Read(new() { ["goal"] = 1L }));

    [Theory]
    [InlineData(-1L)]
    [InlineData(2485L)]
    [InlineData(true)]
    [InlineData("500")]
    [InlineData(500.0)]
    [InlineData(null)]
    public void GraveyardThresholdMustBeAnIntegerInTheSupportedRange(object? value)
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            Read(new() { ["goal"] = 1L, ["graveyard_goal_rating"] = value! }));
        Assert.Contains("graveyard_goal_rating", error.Message);
    }

    private static SlotData Read(Dictionary<string, object> fields)
        => new("Keeper", fields, new FakeLogger());
}
