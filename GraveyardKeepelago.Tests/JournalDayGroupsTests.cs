using GraveyardKeepelago.Journal;
using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class JournalDayGroupsTests
{
    private static JournalEntry At(string timestamp) => new() { Timestamp = DateTimeOffset.Parse(timestamp) };

    [Fact]
    public void EmptyHistoryHasNoDateHeaderAndSameDayNeedsOnlyOne()
    {
        Assert.Empty(JournalDayGroups.Build(Array.Empty<JournalEntry>(), TimeZoneInfo.Utc));
        var groups = JournalDayGroups.Build(new[] { At("2026-10-29T00:00:00Z"), At("2026-10-29T23:59:00Z") }, TimeZoneInfo.Utc).ToArray();
        Assert.Single(groups);
        Assert.Equal("29.10.2026", groups[0].Caption);
        Assert.Equal(2, groups[0].Entries.Count);
    }

    [Fact]
    public void LocalMidnightSeparatesDaysEvenWhenUtcDateIsUnchanged()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Journal test", TimeSpan.FromHours(2), "Test", "Test");
        var entries = new[] { At("2026-10-28T21:59:00Z"), At("2026-10-28T22:00:00Z") };
        var groups = JournalDayGroups.Build(entries, zone).ToArray();
        Assert.Equal(new[] { "28.10.2026", "29.10.2026" }, groups.Select(group => group.Caption));
        Assert.Equal(entries, groups.SelectMany(group => group.Entries));
        Assert.All(entries, entry => Assert.False(entry.Read));
    }

    [Fact]
    public void YearBoundaryAndFilteringDoNotProduceEmptyGroupsOrReorderMessages()
    {
        var first = At("2025-12-31T23:59:00Z");
        var second = At("2026-01-01T00:00:00Z");
        var groups = JournalDayGroups.Build(new[] { first, second }, TimeZoneInfo.Utc).ToArray();
        Assert.Equal(new[] { "31.12.2025", "01.01.2026" }, groups.Select(group => group.Caption));
        Assert.Single(JournalDayGroups.Build(new[] { second }, TimeZoneInfo.Utc));
        Assert.Equal(new[] { first, second, first }, JournalDayGroups.Build(new[] { first, second, first }, TimeZoneInfo.Utc)
            .SelectMany(group => group.Entries));
    }

    [Theory]
    [InlineData("!hint ", true)]
    [InlineData("!hint_location ", true)]
    [InlineData("!missing ", true)]
    [InlineData("!checked ", true)]
    [InlineData("!help ", false)]
    [InlineData("!alias ", false)]
    [InlineData("!hint_location Church", false)]
    public void AcceptedCommandsImmediatelyOfferKnownParameters(string text, bool expected)
    {
        var completion = new JournalCompletion(new[] { "Furnace" }, new[] { "Church" });
        Assert.Equal(expected, completion.HasParameterSuggestions(text));
        if (expected) Assert.NotEmpty(completion.Suggest(text, text.Length, text.Length, text.Length));
    }

    [Fact]
    public void CommandsWithNoKnownNamesDoNotOpenAnEmptyPopup()
        => Assert.False(new JournalCompletion(null, null).HasParameterSuggestions("!hint_location "));

    [Theory]
    [InlineData(187, 187, -150, -150)]
    [InlineData(188, 187, -150, -149)]
    [InlineData(374, 374, -300, -300)]
    public void NativeTechnologyCornerRetainsItsVerticalOffsetAtTheNewDivider(float origin, float top, float target, float expected)
        => Assert.Equal(expected, JournalPanelGeometry.CornerPosition(origin, top, target));
}
