using System.Reflection;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Journal;
using Moq;
using Newtonsoft.Json.Linq;

namespace GraveyardKeepelago.Tests;

public class JournalTests
{
    private static JournalIdentity Party(string seed = "seed", int team = 0, int slot = 1)
        => new() { Seed = seed, Team = team, Slot = slot };
    private static JournalEntry Text(string text, JournalKind kind = JournalKind.Chat)
        => new() { Kind = kind, Parts = new() { new() { Text = text } } };

    [Fact]
    public void DefaultFilterIncludesChatAndSystemButOnlyOurItemTransfers()
    {
        var history = new JournalHistory();
        var chat = Text("hello");
        var system = JournalEntry.System("connected");
        var ours = Text("our item", JournalKind.Item); ours.RelatedToMe = true;
        var other = Text("other item", JournalKind.Item);
        foreach (var entry in new[] { chat, system, ours, other }) history.Add(entry);
        Assert.Equal(new[] { chat, system, ours }, history.Visible(JournalFilter.All, false));
        Assert.Equal(new[] { ours }, history.Visible(JournalFilter.MyItems, true));
        Assert.Equal(new[] { chat }, history.Visible(JournalFilter.Chat, true));
        Assert.Equal(new[] { system }, history.Visible(JournalFilter.System, true));
        Assert.Equal(4, history.Visible(JournalFilter.All, true).Length);
    }

    [Fact]
    public void ReplayIsDeduplicatedButRepeatedChatAndDifferentLocationsSurvive()
    {
        var history = new JournalHistory();
        var first = Text("item", JournalKind.Item); first.TransferKey = "1:42";
        Assert.True(history.Add(first));
        var replay = Text("item", JournalKind.Item); replay.TransferKey = "1:42";
        Assert.False(history.Add(replay));
        var next = Text("item", JournalKind.Item); next.TransferKey = "1:43";
        Assert.True(history.Add(next));
        Assert.True(history.Add(Text("hello")));
        Assert.True(history.Add(Text("hello")));
        Assert.Equal(4, history.Entries.Count);
    }

    [Fact]
    public void OnlyViewedFilterEntriesAreMarkedReadAndUnrelatedItemsDoNotLightIndicator()
    {
        var history = new JournalHistory();
        history.Add(Text("chat"));
        history.Add(JournalEntry.System("lost connection"));
        history.Add(Text("unrelated item", JournalKind.Item));
        Assert.Equal(2, history.Unread(false));
        history.MarkRead(history.Visible(JournalFilter.Chat, false));
        Assert.Equal(1, history.Unread(false));
        Assert.Equal(2, history.Unread(true));
    }

    [Fact]
    public void HistoryKeepsOnlyTheNewestTwoThousandInArrivalOrder()
    {
        var history = new JournalHistory();
        for (var i = 0; i < 2010; i++) history.Add(Text(i.ToString()));
        Assert.Equal(2000, history.Entries.Count);
        Assert.Equal("10", history.Entries[0].Parts[0].Text);
        Assert.Equal("2009", history.Entries[^1].Parts[0].Text);
    }

    [Fact]
    public void ScrollingPreservesReadingPositionAndOnlyFollowsWhenAtBottom()
    {
        var scroll = new JournalScrollPosition();
        scroll.Resize(1000, 300, true);
        Assert.Equal(700, scroll.Offset);
        scroll.Resize(1200, 300, scroll.AtBottom);
        Assert.Equal(900, scroll.Offset);
        scroll.Move(-100);
        scroll.Resize(1300, 300, scroll.AtBottom);
        Assert.Equal(800, scroll.Offset);
        scroll.Resize(1300, 300, false, removedHeight: 40);
        Assert.Equal(760, scroll.Offset);
        scroll.Latest();
        Assert.True(scroll.AtBottom);
        scroll.Move(-10000);
        Assert.Equal(0, scroll.Offset);
    }

    [Fact]
    public void NetworkTextCannotInjectNguiFormattingAndPartsKeepSpacingAndItemFlags()
    {
        var entry = Text("[url=bad][FF0000] hello[-]");
        Assert.DoesNotContain("[url=", entry.Format());
        Assert.Contains("［FF0000］ hello［-］", entry.Format());
        entry.Parts = new() { new() { Text = "Alice", Role = JournalRole.Player },
            new() { Text = " sent " }, new() { Text = "Furnace", Role = JournalRole.Item, Flags = ItemFlags.Advancement } };
        Assert.Contains("Alice[-][/c] sent [c][79439C]Furnace", entry.Format());
    }

    [Fact]
    public void PartyFileNamesAreSafeAndIncludeEveryIdentityField()
    {
        var store = new JournalStore(Path.GetTempPath());
        var identity = Party("../seed:*?\\unsafe");
        Assert.Matches("^[0-9a-f]{64}\\.json$", Path.GetFileName(store.PathFor(identity)));
        var keys = new[] { Party().Key, Party("other").Key, Party(team: 1).Key, Party(slot: 2).Key,
            new JournalIdentity { Game = "Other", Seed = "seed", Team = 0, Slot = 1 }.Key };
        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    [Fact]
    public void StoreRoundTripsReadStateAndDeduplicationAcrossRestartAndIsolatesParties()
    {
        InDirectory(directory =>
        {
            var store = new JournalStore(directory);
            var item = Text("furnace", JournalKind.Item); item.TransferKey = "transfer"; item.Read = true;
            var history = new JournalHistory(); history.Add(item); history.Add(Text("hello"));
            store.Save(Party(), history.Entries);
            var reloaded = new JournalHistory(); reloaded.Load(store.Load(Party()));
            Assert.True(reloaded.Entries[0].Read);
            Assert.False(reloaded.Entries[1].Read);
            Assert.False(reloaded.Add(item));
            Assert.Empty(store.Load(Party("different")));
            Assert.Equal("hello", reloaded.Entries[1].Parts[0].Text);
        });
    }

    [Fact]
    public void CorruptPrimaryCanUseBackupAndCannotMixPartyIdentity()
    {
        InDirectory(directory =>
        {
            var store = new JournalStore(directory);
            store.Save(Party(), new[] { Text("first") });
            store.Save(Party(), new[] { Text("second") });
            File.WriteAllText(store.PathFor(Party()), "broken");
            Assert.Equal("first", store.Load(Party())[0].Parts[0].Text);
            File.Copy(store.PathFor(Party()) + ".bak", store.PathFor(Party("other")));
            Assert.Throws<InvalidDataException>(() => store.Load(Party("other")));
        });
    }

    [Fact]
    public void FailedStorageDoesNotBlockHistoryAndLogsOnlyOncePerConnection()
    {
        var store = new Mock<IJournalStore>();
        store.Setup(value => value.Load(It.IsAny<JournalIdentity>())).Throws(new IOException("private path"));
        var warnings = new List<string>();
        var service = new JournalService(store.Object, warnings.Add);
        service.BeginConnection(); service.Bind(Party());
        service.Enqueue(Text("still works")); service.Pump();
        service.Flush(); service.Flush();
        Assert.Contains(service.History.Entries, entry => entry.Parts[0].Text == "still works");
        Assert.Single(warnings);
        Assert.DoesNotContain("private path", warnings[0]);
    }

    [Fact]
    public void UnwritableStoreIsRetriedOnlyOnNextConnection()
    {
        var store = new Mock<IJournalStore>();
        store.Setup(value => value.Load(It.IsAny<JournalIdentity>())).Returns(Array.Empty<JournalEntry>());
        store.Setup(value => value.Save(It.IsAny<JournalIdentity>(), It.IsAny<IReadOnlyList<JournalEntry>>()))
            .Throws(new UnauthorizedAccessException());
        var warnings = new List<string>();
        var service = new JournalService(store.Object, warnings.Add);
        service.BeginConnection(); service.Bind(Party());
        service.Flush(); service.Enqueue(Text("more")); service.Flush();
        Assert.Single(warnings);
        service.BeginConnection(); service.Bind(Party()); service.Flush();
        Assert.Equal(2, warnings.Count);
    }

    [Fact]
    public async Task InitialBatchWaitsForValidatedIdentityAndPartySwitchDropsOldPendingMessages()
    {
        var store = new Mock<IJournalStore>();
        store.Setup(value => value.Load(It.IsAny<JournalIdentity>())).Returns(Array.Empty<JournalEntry>());
        var service = new JournalService(store.Object, _ => { });
        service.BeginConnection();
        await Task.Run(() => service.Enqueue(Text("initial message")));
        service.Pump(); Assert.Empty(service.History.Entries);
        service.Bind(Party());
        Assert.Equal("initial message", service.History.Entries[0].Parts[0].Text);
        service.Flush();
        service.Enqueue(Text("old party pending"));
        service.BeginConnection(); service.Enqueue(Text("new party")); service.Bind(Party("other"));
        Assert.DoesNotContain(service.History.Entries, entry => entry.Parts[0].Text == "old party pending");
        service.CancelConnection(); service.Enqueue(Text("late callback")); service.Pump();
        Assert.Empty(service.History.Entries);
    }

    [Theory]
    [InlineData("hello", true, true)]
    [InlineData(" !hint Furnace ", true, true)]
    [InlineData("", true, false)]
    [InlineData("   ", true, false)]
    [InlineData("hello\nworld", true, false)]
    [InlineData("hello", false, false)]
    public void ChatSendsOnlyValidatedSingleLinesAndForwardsServerCommands(string text, bool connected, bool expected)
    {
        var sent = new List<string>();
        Assert.Equal(expected, JournalChat.TrySend(text, connected, sent.Add, out var error));
        if (expected) { Assert.Equal(text.Trim(), Assert.Single(sent)); Assert.Empty(error); }
        else { Assert.Empty(sent); Assert.NotEmpty(error); }
    }

    [Fact]
    public void ChatReportsSendFailureWithoutExposingExceptionOrAcceptingOversizedInput()
    {
        Assert.False(JournalChat.TrySend(new string('x', 501), true, _ => Assert.Fail("must not send"), out _));
        Assert.False(JournalChat.TrySend("hello", true, _ => throw new IOException("credentials"), out var error));
        Assert.DoesNotContain("credentials", error);
    }

    [Fact]
    public void RealApAdapterKeepsChatAndSeparatesHintsFromTransferDeduplication()
    {
        var parts = Array.Empty<MessagePart>();
        var chat = Construct<ServerChatLogMessage>(parts, "hello");
        Assert.Equal(JournalKind.Chat, JournalMessageAdapter.Convert(chat).Kind);
        var command = Construct<CommandResultLogMessage>((object)parts);
        Assert.Equal(JournalKind.System, JournalMessageAdapter.Convert(command).Kind);

        var active = new PlayerInfo(0, 1, "Me", "Me", "Graveyard Keeper", Array.Empty<NetworkSlot>(), Array.Empty<int>());
        var friend = new PlayerInfo(0, 2, "Friend", "Friend", "Other", Array.Empty<NetworkSlot>(), Array.Empty<int>());
        var players = new Mock<IPlayerHelper>();
        players.SetupGet(value => value.ActivePlayer).Returns(active);
        players.Setup(value => value.GetPlayerInfo(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int team, int slot) => slot == 1 ? active : friend);
        players.Setup(value => value.GetPlayerInfo(It.IsAny<int>())).Returns((int slot) => slot == 1 ? active : friend);
        var item = new NetworkItem { Item = 100, Location = 200, Player = 2, Flags = ItemFlags.Advancement };
        var transfer = Construct<ItemSendLogMessage>(parts, players.Object, 0, 1, item, Mock.Of<IItemInfoResolver>());
        var converted = JournalMessageAdapter.Convert(transfer);
        Assert.Equal(JournalKind.Item, converted.Kind);
        Assert.True(converted.RelatedToMe);
        Assert.NotNull(converted.TransferKey);
        var hint = Construct<HintItemSendLogMessage>(parts, players.Object, 0, 1, item, false, Mock.Of<IItemInfoResolver>());
        Assert.Equal(JournalKind.System, JournalMessageAdapter.Convert(hint).Kind);
        Assert.Null(JournalMessageAdapter.Convert(hint).TransferKey);
        item.Location = -1;
        var special = Construct<ItemSendLogMessage>(parts, players.Object, 0, 1, item, Mock.Of<IItemInfoResolver>());
        Assert.Null(JournalMessageAdapter.Convert(special).TransferKey);
    }

    [Fact]
    public void TabOrderKeepsNativeTabsAndInsertsJournalImmediatelyAfterMap()
    {
        var order = JournalTabOrder.InsertAfter(new[] { "Inventory", "Techs", "NPCs", "Map", "Other mod" }, "Map", "Archipelago");
        Assert.Equal(new[] { "Inventory", "Techs", "NPCs", "Map", "Archipelago", "Other mod" }, order);
        Assert.Throws<InvalidOperationException>(() => JournalTabOrder.InsertAfter(order, "Map", "Archipelago"));
        Assert.Throws<InvalidOperationException>(() => JournalTabOrder.InsertAfter(new[] { "Inventory" }, "Map", "Archipelago"));
    }

    [Fact]
    public void MismatchedSessionCallbacksCannotEnterTheCurrentPartysHistory()
    {
        var store = new Mock<IJournalStore>();
        store.Setup(value => value.Load(It.IsAny<JournalIdentity>())).Returns(Array.Empty<JournalEntry>());
        var service = new JournalService(store.Object, _ => { });
        service.BeginConnection();
        service.Enqueue(Text("old seed"), Party("old"));
        service.Enqueue(Text("wrong slot"), Party(slot: 2));
        service.Enqueue(Text("wrong team"), Party(team: 1));
        service.Enqueue(Text("initial matching message"), Party());
        service.Bind(Party());
        Assert.Equal(new[] { "initial matching message", "Connected to Archipelago." },
            service.History.Entries.Select(entry => entry.Parts[0].Text));
    }

    [Fact]
    public void RealSessionSubscriptionCapturesInitialBatchAndUnsubscribesOnReconnect()
    {
        var store = new Mock<IJournalStore>();
        store.Setup(value => value.Load(It.IsAny<JournalIdentity>())).Returns(Array.Empty<JournalEntry>());
        var service = new JournalService(store.Object, _ => { });
        var messages = new Mock<IMessageLogHelper>();
        MessageLogHelper.MessageReceivedHandler? oldCallback = null;
        messages.SetupAdd(value => value.OnMessageReceived += It.IsAny<MessageLogHelper.MessageReceivedHandler>())
            .Callback<MessageLogHelper.MessageReceivedHandler>(handler => oldCallback = handler);
        var part = Construct<MessagePart>(MessagePartType.Text, new JsonMessagePart { Text = "same chat" }, null!);
        var chat = Construct<ServerChatLogMessage>(new[] { part }, "same chat");
        service.BeginConnection();
        var subscription = new JournalMessageSubscription(messages.Object, service, Party(), _ => { });
        messages.Raise(value => value.OnMessageReceived += null, chat);
        service.Bind(Party());
        Assert.Contains(service.History.Entries, entry => entry.Parts[0].Text == "same chat");
        subscription.Dispose(); subscription.Dispose();
        messages.VerifyRemove(value => value.OnMessageReceived -= It.IsAny<MessageLogHelper.MessageReceivedHandler>(), Times.Once);
        var before = service.History.Entries.Count;
        oldCallback!(chat); // Simulate an invocation list captured before unsubscription.
        service.Pump();
        Assert.Equal(before, service.History.Entries.Count);
        using var reconnected = new JournalMessageSubscription(messages.Object, service, Party(), _ => { });
        messages.Raise(value => value.OnMessageReceived += null, chat);
        service.Pump();
        Assert.Equal(before + 1, service.History.Entries.Count); // Repeated chat is legitimate.
    }

    [Fact]
    public void InvalidPersistedEntriesAndUnsupportedFormatDoNotBreakTheSession()
    {
        InDirectory(directory =>
        {
            var store = new JournalStore(directory);
            var path = store.PathFor(Party());
            store.Save(Party(), new[] { Text("valid") });
            var json = JObject.Parse(File.ReadAllText(path));
            json["Version"] = 999;
            File.WriteAllText(path, json.ToString());
            Assert.Throws<InvalidDataException>(() => store.Load(Party()));
            json["Version"] = 1;
            json["Entries"]![0]!["Parts"] = null;
            File.WriteAllText(path, json.ToString());
            Assert.Throws<InvalidDataException>(() => store.Load(Party()));
        });
    }

    [Fact]
    public void RealAdapterRetainsServerItemFlagsAndAdjacentTextWithoutAddingSpaces()
    {
        var active = new PlayerInfo(0, 1, "Me", "Me", "Graveyard Keeper", Array.Empty<NetworkSlot>(), Array.Empty<int>());
        var players = new Mock<IPlayerHelper>();
        players.SetupGet(value => value.ActivePlayer).Returns(active);
        players.Setup(value => value.GetPlayerInfo(It.IsAny<int>())).Returns(active);
        var resolver = new Mock<IItemInfoResolver>();
        resolver.Setup(value => value.GetItemName(100, "Graveyard Keeper")).Returns("Furnace");
        var item = Construct<ItemMessagePart>(players.Object, resolver.Object,
            new JsonMessagePart { Type = JsonMessagePartType.ItemId, Text = "100", Player = 1, Flags = ItemFlags.Trap });
        var before = Construct<MessagePart>(MessagePartType.Text, new JsonMessagePart { Text = "Received: " }, null!);
        var after = Construct<MessagePart>(MessagePartType.Text, new JsonMessagePart { Text = "." }, null!);
        var message = Construct<CommandResultLogMessage>((object)new MessagePart[] { before, item, after });
        var entry = JournalMessageAdapter.Convert(message);
        Assert.Equal(JournalRole.Item, entry.Parts[1].Role);
        Assert.Equal(ItemFlags.Trap, entry.Parts[1].Flags);
        Assert.EndsWith("Received: [c][FA8072]Furnace[-][/c].", entry.Format());
    }

    // The library constructs log messages internally. Invoke its real constructors, not fake subclasses.
    private static T Construct<T>(params object[] args) => (T)Activator.CreateInstance(typeof(T),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null)!;

    private static void InDirectory(Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), "gk-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try { test(path); }
        finally { Directory.Delete(path, true); }
    }
}
