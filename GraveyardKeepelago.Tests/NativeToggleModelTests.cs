using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class NativeToggleModelTests
{
    [Theory]
    [InlineData(false, "Disabled", true, "Enabled")]
    [InlineData(true, "Enabled", false, "Disabled")]
    public void SelectorWrapsToTheOtherValue(bool initial, string initialText, bool expected, string expectedText)
    {
        var model = new NativeToggleModel { Value = initial };
        var notifications = new List<bool>();
        model.Changed += notifications.Add;
        Assert.Equal(initialText, model.DisplayText);
        model.Toggle();
        Assert.Equal(expected, model.Value);
        Assert.Equal(expectedText, model.DisplayText);
        Assert.Equal(new[] { expected }, notifications);
        model.Toggle();
        Assert.Equal(initial, model.Value);
        Assert.Equal(new[] { expected, initial }, notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LockedSelectorIgnoresMouseOrKeyboardAction(bool value)
    {
        var model = new NativeToggleModel { Value = value, Editable = false };
        var notifications = 0;
        model.Changed += _ => notifications++;
        model.Toggle();
        Assert.Equal(value, model.Value);
        Assert.Equal(0, notifications);
        model.Editable = true;
        model.Toggle();
        Assert.Equal(!value, model.Value);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void RestoringSavedValueDoesNotReportUserInteraction()
    {
        var model = new NativeToggleModel();
        var notifications = 0;
        model.Changed += _ => notifications++;
        model.Value = true;
        Assert.Equal("Enabled", model.DisplayText);
        Assert.Equal(0, notifications);
    }
}
