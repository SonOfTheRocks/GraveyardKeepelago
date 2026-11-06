using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class ReceiptSourceTextTests
{
    [Theory]
    [InlineData("Friend", 2, 1, "Friend sent you:")]
    [InlineData("Player1", 1, 1, "You found:")]
    [InlineData("Player1", 2, 1, "Player1 sent you:")] // IDs, not display names, identify self.
    [InlineData("Server", 0, 1, "The server sent you:")]
    [InlineData("", 2, 1, "Another player sent you:")]
    public void HeaderIdentifiesSourceWithoutRepeatingTheItem(string player, long sender, long local, string expected)
    {
        var received = new ReceivedItem("Check", "Merchant - Happiness +10", player, 100, 200, sender, 0);
        var notification = ItemNotification.Create(received.ItemName, null!, null!, received, local);
        Assert.Equal(expected, notification.SourceText);
        Assert.DoesNotContain(notification.Name, notification.SourceText);
    }

    [Fact]
    public void SuccessfulApplicationPreservesOriginalSenderForQueuedPresentation()
    {
        var contract = ClientContractLoader.LoadDefault();
        var registry = new GKItemRegistry();
        new GKItemFactory(new FakePlayerActions(), contract).BuildAll(registry);
        var processor = new ItemProcessor(new FakeLogger(), registry, new TrapManagerStub());
        var item = contract.Items.Values.First(definition => definition.Grant.Type == "relation");
        var received = new ReceivedItem("Check", item.Name, "Friend", 100, 200, 2, 0);
        ReceivedItem? captured = null;
        string? header = null;
        processor.ItemApplied = (source, applied) =>
        {
            captured = source;
            header = ItemNotification.Create(source.ItemName, item.Grant, applied, source, 1).SourceText;
        };
        Assert.True(processor.TryProcessItem(received));
        Assert.Same(received, captured);
        Assert.Equal("Friend sent you:", header);
    }
}
