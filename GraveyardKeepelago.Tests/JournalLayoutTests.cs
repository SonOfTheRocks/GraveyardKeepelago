using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Journal;
using Archipelago.MultiClient.Net.Enums;

namespace GraveyardKeepelago.Tests;

public class JournalLayoutTests
{
    [Theory]
    [InlineData(560, 240, 16)]
    [InlineData(720, 480, 16)]
    [InlineData(1440, 960, 32)]
    [InlineData(384, 360, 10)]
    public void FooterTextCannotOverlapInputAndEachZoneHasItsOwnSpace(float width, float height, float line)
    {
        var layout = new JournalLayout(width, height, line, line * 2.5f);
        Assert.True(layout.History.Height > 0);
        Assert.True(layout.Filters[0].Bottom > layout.History.Top);
        Assert.True(layout.History.Bottom > layout.Latest.Top);
        Assert.True(layout.History.Bottom > layout.Status.Top);
        Assert.True(layout.Status.Bottom > layout.Input.Top);
        Assert.True(layout.Latest.Bottom > layout.Send.Top);
        Assert.True(layout.Input.Bottom > layout.Help.Top);
        Assert.True(layout.Send.Bottom > layout.Help.Top);
        Assert.True(layout.Help.Bottom > -height / 2);
        Assert.True(layout.Input.Width > width * 0.5f);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    public void ControlsAndColumnsShareEdgesAndButtonCenters(float line)
    {
        var layout = new JournalLayout(line * 45, line * 30, line, line * 3);
        Assert.Equal(layout.History.X, layout.Filters[0].X);
        Assert.Equal(layout.History.X, layout.Status.X);
        Assert.Equal(layout.History.X, layout.Input.X);
        Assert.Equal(layout.History.X, layout.Help.X);
        Assert.Equal(layout.Scrollbar.Right, layout.OtherItems.Right);
        Assert.Equal(layout.History.Top, layout.Scrollbar.Top);
        Assert.Equal(layout.History.Bottom, layout.Scrollbar.Bottom);
        Assert.Equal(layout.History.Right + layout.Gap, layout.Scrollbar.X, 3);
        Assert.Equal(layout.OtherItems.Right, layout.Send.Right);
        Assert.Equal(layout.Send.X, layout.Latest.X);
        Assert.Equal(layout.Send.CenterY, layout.Input.CenterY);
        Assert.Equal(layout.Latest.CenterY, layout.Status.CenterY);
        Assert.Equal(layout.Filters[0].CenterY, layout.OtherItems.CenterY);
        Assert.Equal(layout.History.Right, layout.BodyLeft + layout.BodyWidth);
        Assert.True(layout.TimeRight < layout.BodyLeft);
        for (var i = 1; i < layout.Filters.Length; i++)
            Assert.True(layout.Filters[i - 1].Right < layout.Filters[i].X);
        Assert.True(layout.Filters[^1].Right < layout.OtherItems.X);
    }

    [Fact]
    public void WrappedMessagesHaveASeparateTimeGutterAndUseTheDarkBackgroundPalette()
    {
        var entry = new JournalEntry
        {
            Parts = new() { new() { Text = "Alice", Role = JournalRole.Player }, new() { Text = " sent " },
                new() { Text = "Furnace", Role = JournalRole.Item, Flags = ItemFlags.Advancement } },
        };
        var body = entry.FormatBody(lightBackground: false);
        Assert.DoesNotContain(entry.Timestamp.ToLocalTime().ToString("HH:mm"), body);
        Assert.Contains("[AF99EF]Furnace", body);
        Assert.Contains("[FFE0A0]Alice", body);
        Assert.Contains("[79439C]Furnace", entry.Format()); // Existing light-background formatting stays available.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidMetricsAreRejectedInsteadOfProducingOverlappingWidgets(float line)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new JournalLayout(720, 480, line, 40));
}
