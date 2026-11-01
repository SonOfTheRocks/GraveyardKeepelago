using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class ItemNotificationTests
{
    private static ItemNotification Receipt(string name) => new(name, Array.Empty<(string, string)>());

    [Fact]
    public void PermanentBuffReceiptsUseTheirBuffDefinitionInsteadOfAnEmptyIconRow()
    {
        var contract = ClientContractLoader.LoadDefault();
        var buffs = contract.Items.Values.Where(item => item.Grant.Type == "permanent_buff").ToList();
        Assert.NotEmpty(buffs);
        foreach (var buff in buffs)
        {
            var receipt = ItemNotification.Create(buff.Name, buff.Grant, null!);
            Assert.Equal(new[] { ("buff", buff.Grant.BuffID) }, receipt.Entries);
        }
        var toughMood = contract.Items["ITEM_PBUFF_TOUGH_MOOD"];
        Assert.Equal(new[] { ("buff", "buff_shield") },
            ItemNotification.Create(toughMood.Name, toughMood.Grant, null!).Entries);
    }

    private static void Advance(ItemNotificationQueue feed, float seconds, bool canShow = true)
    {
        for (var i = 0; i < (int)Math.Round(seconds * 100); i++) feed.Update(0.01f, canShow);
    }

    [Theory]
    [InlineData(global::Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement, "AF99EF")]
    [InlineData(global::Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude, "6D8BE8")]
    [InlineData(global::Archipelago.MultiClient.Net.Enums.ItemFlags.Trap, "FA8072")]
    [InlineData(global::Archipelago.MultiClient.Net.Enums.ItemFlags.None, "00EEEE")]
    public void HudTextPreservesCategoryColorsButTreatsNetworkNamesAsLiteralText(
        global::Archipelago.MultiClient.Net.Enums.ItemFlags flags, string color)
    {
        var receipt = new ItemNotification("[url=bad]Long item[-]", Array.Empty<(string, string)>(),
            "[00]Sender sent you:", flags: flags);
        Assert.Equal("[c][DDDDDD]［00］Sender sent you:[-][/c]\n" +
            "[c][" + color + "]［url=bad］Long item［-］[-][/c]", receipt.HudText);
        Assert.DoesNotContain("[url", receipt.HudText);
    }

    [Fact]
    public void NotificationsFadeInAndResumeMidFadeAfterAConversation()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("First"));
        feed.Update(0, true);
        Assert.Equal(0, feed.Visible[0].Opacity);
        Advance(feed, 0.1f);
        Assert.InRange(feed.Visible[0].Opacity, 0.49f, 0.51f);
        Advance(feed, 0.2f);
        Assert.Equal(1, feed.Visible[0].Opacity);
        Advance(feed, ItemNotificationQueue.Lifetime - 0.8f);
        var opacity = feed.Visible[0].Opacity;
        Advance(feed, 30, false);
        Assert.Equal(opacity, feed.Visible[0].Opacity);
        Advance(feed, 0.6f);
        Assert.Empty(feed.Visible);
    }
    [Theory]
    [InlineData("Loading")]
    [InlineData("Control disabled")]
    [InlineData("Menu")]
    [InlineData("Conversation")]
    [InlineData("Cutscene")]
    [InlineData("Fishing/combat/work")]
    public void BlockedGameplayRetainsReceiptUntilTheActivityFinishes(string activity)
    {
        var context = new ItemNotificationContext { GameReady = true, PlayerControllable = true };
        switch (activity)
        {
            case "Loading": context.GameReady = false; break;
            case "Control disabled": context.PlayerControllable = false; break;
            case "Menu": context.WindowOpen = true; break;
            case "Conversation": context.Talking = true; break;
            case "Cutscene": context.Cinematic = true; break;
            default: context.Busy = true; break;
        }
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("Waiting"));
        Advance(feed, 10, context.CanShow);
        Assert.Empty(feed.Visible);
        Assert.Equal(1, feed.PendingCount);
        context.GameReady = context.PlayerControllable = true;
        context.WindowOpen = context.Talking = context.Cinematic = context.Busy = false;
        feed.Update(0, context.CanShow);
        Assert.Equal("Waiting", Assert.Single(feed.Visible).Notification.Name);
    }

    [Fact]
    public void BatchAppearsInOrderWithTimeToReadAndNewestAtTheBottom()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("First"));
        feed.Enqueue(Receipt("Second"));
        feed.Update(0, true);
        Advance(feed, 0.6f);
        Assert.Equal("First", Assert.Single(feed.Visible).Notification.Name);
        Advance(feed, 0.7f);
        Assert.Equal(new[] { "First", "Second" }, feed.Visible.Select(row => row.Notification.Name));
        Assert.Equal(0, feed.PendingCount);
    }

    [Fact]
    public void FullStackWaitsRatherThanDroppingOrPrematurelyExpiringItems()
    {
        var feed = new ItemNotificationQueue();
        for (var i = 0; i < 10; i++) feed.Enqueue(Receipt(i.ToString()));
        feed.Update(0, true);
        Advance(feed, 5);
        Assert.Equal(4, feed.Visible.Count);
        Assert.Equal(6, feed.PendingCount);
        var seen = new HashSet<string>(feed.Visible.Select(row => row.Notification.Name));
        for (var i = 0; i < 5000; i++)
        {
            feed.Update(0.01f, true);
            Assert.InRange(feed.Visible.Count, 0, ItemNotificationQueue.Capacity);
            foreach (var row in feed.Visible) seen.Add(row.Notification.Name);
        }
        Assert.Equal(10, seen.Count);
        Assert.Empty(feed.Visible);
        Assert.Equal(0, feed.PendingCount);
    }

    [Fact]
    public void ConversationHidesButDoesNotAgeVisibleItemsOrAdvanceThePendingBatch()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("First"));
        feed.Update(0, true);
        Advance(feed, 1);
        feed.Enqueue(Receipt("Next"));
        var age = feed.Visible[0].Age;
        Advance(feed, 60, false);
        Assert.Equal(age, Assert.Single(feed.Visible).Age);
        Assert.Equal(1, feed.PendingCount);
        Advance(feed, 0.3f);
        Assert.Equal(2, feed.Visible.Count);
    }

    [Fact]
    public void EachRepeatedItemRemainsSeparateAndFadesBeforeExpiring()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("Same"));
        feed.Enqueue(Receipt("Same"));
        feed.Update(0, true);
        Advance(feed, ItemNotificationQueue.Lifetime - 0.5f);
        Assert.Equal(2, feed.Visible.Count);
        Assert.InRange(feed.Visible[0].Opacity, 0.5f, 0.8f);
        Assert.Equal(1, feed.Visible[1].Opacity);
        Advance(feed, 0.6f);
        Assert.Single(feed.Visible);
        Advance(feed, 1.3f);
        Assert.Empty(feed.Visible);
    }

    [Fact]
    public void EachReceiptRemainsFullyReadableForTenSecondsEvenWhenNewItemsArrive()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("First"));
        feed.Update(0, true);
        Advance(feed, 0.25f);
        var first = Assert.Single(feed.Visible);
        feed.Enqueue(Receipt("Next"));
        Advance(feed, 5);
        Assert.Contains(first, feed.Visible);
        Assert.Equal(1, first.Opacity);
        Advance(feed, 4.9f);
        Assert.Contains(first, feed.Visible);
        Assert.Equal(1, first.Opacity);
        Advance(feed, 0.2f);
        Assert.InRange(first.Opacity, 0.01f, 0.99f);
        Advance(feed, 0.7f);
        Assert.DoesNotContain(first, feed.Visible);
    }
    [Fact]
    public void FrameStallDoesNotExpireUnseenMessagesOrFloodTheFeed()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("First"));
        feed.Enqueue(Receipt("Second"));
        feed.Update(0, true);
        feed.Update(30, true);
        Assert.Equal(0.1f, Assert.Single(feed.Visible).Age);
        Assert.Equal(1, feed.PendingCount);
    }

    [Fact]
    public void ClearDropsVisibleAndPendingReceiptsAndResetsBatchPacing()
    {
        var feed = new ItemNotificationQueue();
        feed.Enqueue(Receipt("Old save"));
        feed.Update(0, true);
        feed.Enqueue(Receipt("Old pending"));
        feed.Clear();
        Assert.Empty(feed.Visible);
        Assert.Equal(0, feed.PendingCount);
        feed.Enqueue(Receipt("New save"));
        feed.Update(0, true);
        Assert.Equal("New save", Assert.Single(feed.Visible).Notification.Name);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProgressiveReceiptKeepsItsStageWhileLaterItemsAreApplied(bool craft)
    {
        var actions = new FakePlayerActions();
        var stages = new List<List<string>> { new() { "first", "@hidden" }, new() { "second" } };
        var grant = new ClientContractGrant
            { Type = craft ? "progressive_craft_unlock" : "progressive_work_unlock", Stages = stages };
        GKProgressiveItem item = craft ? new GKProgressiveRecipe(stages, actions) : new GKProgressiveWork(stages, actions);
        item.Apply();
        var first = ItemNotification.Create("Progressive", grant, item);
        item.Apply();
        var second = ItemNotification.Create("Progressive", grant, item);
        Assert.Equal(new[] { (craft ? "craft" : "work", "first") }, first.Entries);
        Assert.Equal(new[] { (craft ? "craft" : "work", "second") }, second.Entries);
    }

    [Fact]
    public void CompositeReceiptIncludesAllVisibleUnlocksWithoutTheVanillaThreeRowLimit()
    {
        var grant = new ClientContractGrant
        {
            CraftIDs = new() { "recipe", "@hidden" }, WorkIDs = new() { "work" },
            PerkID = "perk", PerkIDs = new() { "other perk" },
        };
        var receipt = ItemNotification.Create("Composite", grant, null!);
        Assert.Equal(new[] { ("craft", "recipe"), ("work", "work"),
            ("perk", "other perk"), ("perk", "perk") }, receipt.Entries);
    }

    [Fact]
    public void EveryContractItemEmitsExactlyOneNotificationAfterSuccessfulApplication()
    {
        var contract = ClientContractLoader.LoadDefault();
        var registry = new GKItemRegistry();
        var actions = new FakePlayerActions();
        new GKItemFactory(actions, contract).BuildAll(registry);
        var processor = new ItemProcessor(new FakeLogger(), registry, new TrapManagerStub());
        var notified = new List<string>();
        processor.ItemApplied = (received, item) =>
        {
            var name = received.ItemName;
            Assert.NotNull(actions.LastFunctionCalled);
            var grant = contract.Items.Values.Single(definition => definition.Name == name).Grant;
            var receipt = ItemNotification.Create(name, grant, item);
            Assert.Equal(name, receipt.Name);
            notified.Add(name);
        };
        foreach (var item in contract.Items.Values)
            processor.ProcessItem(new ReceivedItem("dummy", item.Name, "dummy", 0, 0, 0, 0));
        Assert.Equal(contract.Items.Count, notified.Count);
        Assert.Equal(contract.Items.Count, notified.Distinct().Count());
    }

    [Fact]
    public void UnknownOrFailedItemsDoNotReportSuccessfulUnlocks()
    {
        var registry = new GKItemRegistry();
        registry.Add("Broken", new ThrowingItem());
        var processor = new ItemProcessor(new FakeLogger(), registry, new TrapManagerStub())
            { ItemApplied = (_, _) => Assert.Fail("Unsuccessful receipt was announced") };
        processor.ProcessItem(new ReceivedItem("dummy", "Unknown", "dummy", 0, 0, 0, 0));
        Assert.Throws<InvalidOperationException>(() =>
            processor.ProcessItem(new ReceivedItem("dummy", "Broken", "dummy", 0, 0, 0, 0)));
    }

    [Fact]
    public void NotificationFailureDoesNotFailOrReapplyTheReward()
    {
        var actions = new FakePlayerActions();
        var registry = new GKItemRegistry();
        var item = new GKProgressiveRecipe(new() { new() { "recipe" } }, actions);
        registry.Add("Progressive", item);
        var logger = new FakeLogger();
        var processor = new ItemProcessor(logger, registry, new TrapManagerStub())
            { ItemApplied = (_, _) => throw new InvalidOperationException("UI failure") };
        processor.ProcessItem(new ReceivedItem("dummy", "Progressive", "dummy", 0, 0, 0, 0));
        Assert.Equal(1, item.applyCount);
        Assert.Single(logger.ErrorLogs);
    }

    private sealed class ThrowingItem : IAPItem
    {
        public void Apply() => throw new InvalidOperationException();
    }
}




