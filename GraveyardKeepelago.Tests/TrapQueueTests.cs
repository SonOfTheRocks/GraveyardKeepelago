using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class TrapQueueTests
{
    private static SaveProgress NewProgress() => new() { Seed = "trap-seed", Slot = 1, Team = 0 };
    private static ReceivedItem Receipt(string name, int index) => new("Test check", name, "Player1", 100 + index,
        name == "Bats Trap" ? 488 : name == "Green Slime Trap" ? 489 : 999, 1, index);

    [Theory]
    [InlineData("Bats Trap", "bats")]
    [InlineData("Green Slime Trap", "green_slimes")]
    public void UnsafeReceiptIsDurableAndDoesNotBlockLaterNormalItemsOrReplayOnReload(string trapName, string trapID)
    {
        var state = NewProgress();
        var records = new List<string>();
        var ready = false;
        var now = 0f;
        var executions = 0;
        var queue = new TrapManager(new FakeLogger(), state, () => state.Store(records),
            id => { Assert.Equal(trapID, id); executions++; }, () => ready, () => now);
        var actions = new FakePlayerActions();
        actions.TrapQueueCallback = queue.Enqueue;
        var registry = new GKItemRegistry();
        new GKItemFactory(actions).BuildAll(registry);
        registry.Add("Normal item", new QueuedTrap(() => actions.AddResearchPoints("red", 5)));
        var processor = new ItemProcessor(new FakeLogger(), registry, queue);
        var notifications = new List<string>();
        processor.ItemApplied = (receipt, _) => notifications.Add(receipt.ItemName);
        var ledger = new ItemReceiptLedger(state, () => state.Store(records));
        var history = new[] { Receipt(trapName, 0), Receipt("Normal item", 1) };
        ledger.ApplyHistory(history, processor.TryProcessItem);
        Assert.Equal(2, state.Receipts.Count);
        Assert.Equal("AddResearchPoints", actions.LastFunctionCalled);
        Assert.Equal(new[] { trapName, "Normal item" }, notifications);
        Assert.Equal(new[] { trapID }, state.PendingTraps);
        queue.DequeueTrap();
        Assert.Equal(0, executions);

        var restored = SaveProgress.Load(records, "trap-seed", 0, 1);
        var restoredQueue = new TrapManager(new FakeLogger(), restored, () => restored.Store(records),
            id => { Assert.Equal(trapID, id); executions++; }, () => ready, () => now);
        new ItemReceiptLedger(restored, () => restored.Store(records)).ApplyHistory(history,
            _ => throw new Exception("A recorded receipt was replayed"));
        Assert.Single(restored.PendingTraps);
        ready = true;
        now = 20;
        restoredQueue.DequeueTrap();
        restoredQueue.DequeueTrap();
        Assert.Equal(1, executions);
        Assert.Empty(restored.PendingTraps);
        Assert.Empty(SaveProgress.Load(records, "trap-seed", 0, 1).PendingTraps);
    }

    [Fact]
    public void MultipleTrapsWaitForSafeGameplayAndRespectCooldown()
    {
        var progress = NewProgress();
        var now = 0f;
        var context = new ItemNotificationContext { GameReady = true, PlayerControllable = true, WindowOpen = true };
        var calls = 0;
        var queue = new TrapManager(new FakeLogger(), progress, () => { }, _ => calls++, () => context.CanShow, () => now);
        queue.Enqueue("bats");
        queue.Enqueue("green_slimes");
        now = 50;
        queue.DequeueTrap();
        Assert.Equal(0, calls);
        context.WindowOpen = false;
        queue.DequeueTrap();
        Assert.Equal(0, calls); // safe transition grace period
        now = 52;
        queue.DequeueTrap();
        queue.DequeueTrap();
        Assert.Equal(1, calls);
        now = 62;
        queue.DequeueTrap();
        Assert.Equal(2, calls);
        Assert.Empty(progress.PendingTraps);
    }

    [Fact]
    public void PartialSpawnFailureIsLoggedAndConsumedWithoutBlockingTheNextTrap()
    {
        var progress = NewProgress();
        var now = 0f;
        var logger = new FakeLogger();
        var calls = 0;
        var queue = new TrapManager(logger, progress, () => { },
            _ => { if (++calls == 1) throw new Exception("Partial world effect"); }, () => true, () => now);
        queue.Enqueue("bats");
        queue.Enqueue("green_slimes");
        now = 10;
        queue.DequeueTrap();
        Assert.Single(logger.ErrorLogs);
        Assert.Single(progress.PendingTraps);
        now = 20;
        queue.DequeueTrap();
        Assert.Equal(2, calls);
        Assert.Empty(progress.PendingTraps);
    }

    [Fact]
    public void FailedConsumptionSaveDoesNotExecuteOrLoseTheTrap()
    {
        var progress = NewProgress();
        var now = 0f;
        var calls = 0;
        var queue = new TrapManager(new FakeLogger(), progress, () => throw new IOException("Cannot store"),
            _ => calls++, () => true, () => now);
        queue.Enqueue("bats");
        now = 10;
        Assert.Throws<IOException>(queue.DequeueTrap);
        Assert.Single(progress.PendingTraps);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void OldSaveRecordsStartWithAnEmptyTrapQueue()
    {
        var state = NewProgress();
        var record = Newtonsoft.Json.Linq.JObject.FromObject(state);
        record.Remove(nameof(SaveProgress.PendingTraps));
        var text = SaveProgress.RecordPrefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(record.ToString()));
        Assert.Empty(SaveProgress.Load(new List<string> { text }, "trap-seed", 0, 1).PendingTraps);
    }

    [Fact]
    public void UnknownTrapIdsFailClearlyAndNeverEnterTheQueue()
    {
        var progress = NewProgress();
        var queue = new TrapManager(new FakeLogger(), progress, () => { }, _ => { }, () => true, () => 0);
        Assert.Throws<InvalidDataException>(() => queue.Enqueue("unknown"));
        Assert.False(queue.TryExecuteTrapImmediately("unknown"));
        Assert.Empty(progress.PendingTraps);
        progress.PendingTraps.Add("unknown");
        Assert.Throws<InvalidDataException>(() => new TrapManager(new FakeLogger(), progress,
            () => { }, _ => { }, () => true, () => 0));
    }

    [Theory]
    [InlineData("ITEM_BATS_TRAP", "bats", "Bats Trap")]
    [InlineData("ITEM_GREEN_SLIME_TRAP", "green_slimes", "Green Slime Trap")]
    public void TrapReceiptPreservesTheSpecificTrapAndOriginalApName(string key, string trapID, string name)
    {
        var definition = ClientContractLoader.LoadDefault().Items[key];
        Assert.Equal("TRAP", definition.Classification);
        Assert.Equal("trap", definition.Grant.Type);
        Assert.Equal(trapID, definition.Grant.TrapID);
        var receipt = ItemNotification.Create(definition.Name, definition.Grant, null!);
        Assert.Equal(new[] { ("trap", trapID) }, receipt.Entries);
        Assert.Equal(name, receipt.EntryName("trap", trapID));
    }

    [Fact]
    public void RemovedGeneralMonsterTrapIsNotRecognizedOrQueued()
    {
        var progress = NewProgress();
        var queue = new TrapManager(new FakeLogger(), progress, () => { }, _ => Assert.Fail("Executed too soon"),
            () => false, () => 0);
        foreach (var name in new[] { "Monsters", "Monsters Trap", "Monsters_Trap" })
        {
            Assert.False(queue.IsTrap(name));
            Assert.False(queue.TryExecuteTrapImmediately(name));
        }
        Assert.Empty(progress.PendingTraps);
        Assert.Throws<InvalidDataException>(() => MonsterTrapDefinition.Get("monsters"));
        Assert.Throws<InvalidDataException>(() => queue.Enqueue("monsters"));
        Assert.Empty(progress.PendingTraps);
        progress.PendingTraps.Add("monsters");
        Assert.Throws<InvalidDataException>(() => new TrapManager(new FakeLogger(), progress,
            () => { }, _ => { }, () => false, () => 0));
    }

    [Fact]
    public void MixedPendingTrapsRetainTheirExactTypesAndOrderAfterReload()
    {
        var state = NewProgress();
        state.PendingTraps.AddRange(new[] { "bats", "green_slimes", "bats" });
        var records = new List<string>();
        state.Store(records);
        var restored = SaveProgress.Load(records, "trap-seed", 0, 1);
        var spawned = new List<string>();
        var now = 0f;
        var queue = new TrapManager(new FakeLogger(), restored, () => restored.Store(records),
            spawned.Add, () => true, () => now);
        foreach (var time in new[] { 5f, 15f, 25f }) { now = time; queue.DequeueTrap(); }
        Assert.Equal(new[] { "bats", "green_slimes", "bats" }, spawned);
        Assert.Empty(restored.PendingTraps);
    }

    private sealed class QueuedTrap(Action action) : IAPItem
    {
        public void Apply() => action();
    }
}
