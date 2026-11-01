using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class JournalInputAndHudTests
{
    [Theory]
    [InlineData("Inventory")]
    [InlineData("Techs")]
    [InlineData("KnownNPCs")]
    [InlineData("Map")]
    [InlineData("GameGUI")]
    [InlineData("PrevTab")]
    [InlineData("NextTab")]
    [InlineData("PrevSubTab")]
    [InlineData("NextSubTub")]
    public void FocusedInputOwnsMenuShortcuts(string key)
    {
        Assert.True(TextInputShortcutPolicy.Block(true, key));
        Assert.False(TextInputShortcutPolicy.Block(false, key));
    }

    [Theory]
    [InlineData("Select")]
    [InlineData("Back")]
    [InlineData("LeftClick")]
    [InlineData("IngameMenu")]
    public void NativeSubmitCancelAndMouseRemainAvailable(string key)
        => Assert.False(TextInputShortcutPolicy.Block(true, key));

    [Theory]
    [InlineData(.4f, -.01f, 1.01f, .04f)]
    [InlineData(-.05f, .01f, .8f, .07f)]
    [InlineData(.2f, .03f, .98f, .1f)]
    [InlineData(.1f, .95f, .9f, 1.01f)]
    public void ScaledUnreadLabelStaysInsideScreen(float left, float bottom, float right, float top)
    {
        var delta = JournalHudBounds.Correction(left, bottom, right, top);
        Assert.True(left + delta.X >= .01999f);
        Assert.True(bottom + delta.Y >= .01999f);
        Assert.True(right + delta.X <= .98001f);
        Assert.True(top + delta.Y <= .98001f);
    }

    [Fact]
    public void NativeDividerHasClearanceFromMessagesAndFooterControls()
    {
        var layout = new JournalLayout(720, 480, 16, 40);
        Assert.True(layout.History.Bottom < layout.History.Top);
        Assert.True(layout.History.Bottom > layout.FooterDividerY);
        Assert.True(layout.FooterDividerY > layout.Latest.Top);
        Assert.True(layout.FooterDividerY > layout.Status.Top);
    }
}

