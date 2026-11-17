using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Grants;
using APItemManager = GraveyardKeepelago.Items.Receiving.APItemManager;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Logic;
using KaitoKid.Utilities.Interfaces;
using Moq;
using static GraveyardKeepelago.Tests.ReceivedItemHistoryTests;

namespace GraveyardKeepelago.Tests;

public sealed class APItemManagerReceiptTests
{
    private static readonly (string Seed, int Team, int Slot) Identity = ("Seed", 0, 1);
    private static readonly Dictionary<string, long> Locations = new() { ["Research"] = 17 };

    [Fact]
    public void InitialItemsWaitForReadinessAndRejectAChangedSessionMidBatch()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1), Item(2) };
        var active = false;
        var grants = new List<string>();

        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            grants.Add(name);
            active = false;
            return GrantResult.Applied;
        }

        Process(delivery, history, () => active, Name, Apply);
        Assert.Empty(grants);

        active = true;
        Process(delivery, history, () => active, Name, Apply);
        Assert.Equal("Item 1", Assert.Single(grants));
        Assert.Single(save.Data.Receipts);

        active = true;
        Process(delivery, history, () => active, Name, Apply);
        Assert.Equal(new[] { "Item 1", "Item 2" }, grants);
    }

    [Fact]
    public void ReloadAndReplaySkipConfirmedIndicesButPreserveProgressiveStages()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var stages = new List<int>();
        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            stages.Add(count);
            return GrantResult.Applied;
        }

        var delivery = CreateManager(save);
        Process(delivery, [Item(1), Item(1)], Ready, Name, Apply);
        Process(delivery, [Item(1), Item(1)], Ready, Name, Apply);

        var reloaded = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        Process(CreateManager(reloaded), [Item(1), Item(1), Item(1)], Ready, Name, Apply);
        Assert.Equal(new[] { 0, 1, 2 }, stages);
        Assert.Equal(new[] { 0, 1, 2 }, reloaded.Data.Receipts.Select(receipt => receipt.Index));
    }

    [Theory]
    [InlineData(2, -2, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, -2, 2)]
    public void ConflictingPastReceiptPreventsAnyNewGrant(long id, long location, int player)
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var delivery = CreateManager(save);
        Process(delivery, [Item(1)], Ready, Name, Applied);

        Assert.Throws<InvalidDataException>(() =>
            Process(delivery, [Item(id, location, player), Item(3)], Ready, Name, UnexpectedGrant));
        Assert.Throws<InvalidDataException>(() => Process(delivery, [], Ready, Name, UnexpectedGrant));
        Assert.Single(save.Data.Receipts);
    }

    [Fact]
    public void ExplicitFailureRetriesTheSameStageBeforeDeliveringLaterItems()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        Process(CreateManager(save), [Item(1)], Ready, Name, Applied);

        var attempts = new List<int>();
        var history = new[] { Item(1), Item(1), Item(1) };
        var delivery = CreateManager(save);
        Process(delivery, history, Ready, Name, (_, stage, _) =>
        {
            attempts.Add(stage);
            return GrantResult.Retry;
        });
        Assert.Single(SaveProgress.Bind(records, Identity, newSave: false).Data.Receipts);

        Process(delivery, history, Ready, Name, (_, stage, _) =>
        {
            attempts.Add(stage);
            return GrantResult.Applied;
        });
        Assert.Equal(new[] { 1, 1, 2 }, attempts);
    }

    [Fact]
    public void UncertainNativeFailureStopsTheSameSaveAfterReconnectAndReload()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1) };

        Assert.Throws<IOException>(() =>
            Process(delivery, history, Ready, Name, (_, _, _) => throw new IOException("Partly applied")));
        Assert.Empty(save.Data.Receipts);
        Assert.Throws<InvalidDataException>(() => Process(delivery, history, Ready, Name, UnexpectedGrant));
        var reloaded = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        Assert.Throws<InvalidDataException>(() =>
            Process(CreateManager(reloaded), history, Ready, Name, UnexpectedGrant));
    }

    [Fact]
    public void AsyncGrantWaitsForCompletionAndBlocksLaterItems()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1), Item(2) };
        var finished = false;
        var grants = new List<string>();
        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            grants.Add(name);
            return finished ? GrantResult.Applied : GrantResult.Pending;
        }

        Process(delivery, history, Ready, Name, Apply);
        Assert.Equal("Item 1", Assert.Single(grants));
        Assert.Empty(save.Data.Receipts);
        var during = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        Assert.Throws<InvalidDataException>(() =>
            Process(CreateManager(during), history, Ready, Name, UnexpectedGrant));

        finished = true;
        Process(delivery, history, Ready, Name, Apply);
        Assert.Equal(new[] { "Item 1", "Item 1", "Item 2" }, grants);
        Assert.Equal(2, save.Data.Receipts.Count);
        Assert.Null(save.Data.InterruptedReceipt);
    }

    [Fact]
    public void APartialRecordWriteCannotConfirmOrReplayAnAppliedGrant()
    {
        var records = new FailingRecordList();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1) };
        Assert.Throws<IOException>(() => Process(delivery, history, Ready, Name, (_, _, queueTrap) =>
        {
            queueTrap("bats");
            records.FailWrites = true;
            return GrantResult.Applied;
        }));
        records.FailWrites = false;

        var reloaded = SaveProgress.Bind(records, Identity, newSave: false);
        Assert.Empty(reloaded.Data.PendingTraps);
        Assert.Empty(reloaded.Data.Receipts);
        Assert.Equal(0, reloaded.Data.InterruptedReceipt);
        Assert.Throws<InvalidDataException>(() => Process(delivery, history, Ready, Name, UnexpectedGrant));
    }

    [Fact]
    public void TrapQueueAndReceiptsShareTheCheckRecordAndSurviveReloadInOrder()
    {
        var records = new List<string>();
        var checks = SaveBoundLocationChecks.Bind(records, Identity, Locations, newSave: true);
        var history = new[] { Item(1), Item(2), Item(3) };
        Process(CreateManager(checks.Progress), history, Ready, Name, (name, _, queueTrap) =>
        {
            if (name == "Item 1") queueTrap("bats");
            if (name == "Item 3") queueTrap("green_slimes");
            return GrantResult.Applied;
        });

        checks.AddCheckedLocation("Research");
        Assert.Single(records);
        var reloaded = SaveBoundLocationChecks.Bind(records.ToList(), Identity, Locations, newSave: false);
        Process(CreateManager(reloaded.Progress), history, Ready, Name, UnexpectedGrant);
        Assert.Equal(new[] { "bats", "green_slimes" }, reloaded.Progress.Data.PendingTraps);
        Assert.Equal("Research", Assert.Single(reloaded.Progress.Data.Checks));
        Assert.Equal(3, reloaded.Progress.Data.Receipts.Count);
    }

    [Fact]
    public void FailureToRecordTheStartOfAnAttemptCanBeRetriedWithoutAnyGrantHavingRun()
    {
        var records = new FailingRecordList();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1) };

        records.FailWrites = true;
        Assert.Throws<IOException>(() => Process(delivery, history, Ready, Name, UnexpectedGrant));
        Assert.Null(save.Data.InterruptedReceipt);

        records.FailWrites = false;
        Process(delivery, history, Ready, Name, Applied);
        Assert.Single(save.Data.Receipts);
    }

    [Fact]
    public void PendingGrantRetainsItsInstanceAndStopsWhenTheSaveCloses()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var creationCount = 0;
        var grant = new TestGrant("Item 1", 0);
        var manager = new APItemManager(save, (_, _) =>
        {
            creationCount++;
            return grant;
        });

        GrantResult Apply(IItemGrant current, Action<string> queueTrap)
        {
            Assert.Same(grant, current);
            return GrantResult.Pending;
        }

        manager.ProcessReceivedItems([Item(1)], Ready, Name, Apply);
        manager.ProcessReceivedItems([Item(1)], Ready, Name, Apply);
        Assert.Equal(1, creationCount);
        Assert.True(manager.HasPendingGrant);

        manager.StopActiveGrant();
        Assert.True(grant.Disposed);
        Assert.False(manager.HasPendingGrant);
        Assert.Empty(save.Data.Receipts);
        Assert.Equal(0, SaveProgress.Bind(records, Identity, newSave: false).Data.InterruptedReceipt);
    }

    [Fact]
    public void ARejectedTrapCanBeRetriedBeforeTheNextItem()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var delivery = CreateManager(save);
        var history = new[] { Item(1), Item(2) };

        Process(delivery, history, Ready, Name, (name, _, _) =>
        {
            Assert.Equal("Item 1", name);
            return GrantResult.Retry;
        });
        Assert.Empty(save.Data.Receipts);
        Assert.Empty(save.Data.PendingTraps);

        Process(delivery, history, Ready, Name, (name, _, queueTrap) =>
        {
            if (name == "Item 1") queueTrap("bats");
            return GrantResult.Applied;
        });
        Assert.Equal(2, save.Data.Receipts.Count);
        Assert.Equal("bats", Assert.Single(save.Data.PendingTraps));
    }

    [Fact]
    public void ProgressiveReceiptSelectsTheCorrectConcreteGrantAfterReload()
    {
        var record = new List<string>();
        var item = ClientContractLoader.LoadDefault().Items["ITEM_LEARN_GATHERING_PROGRESSIVE_TREE_FELLING"];
        var factory = new ItemGrantFactory([item], Mock.Of<ILogger>());
        var stages = new List<string>();

        GrantResult Apply(IItemGrant grant, Action<string> queueTrap)
        {
            Assert.True(ItemProcessor.IsApplyingItem);
            stages.Add(Assert.Single(Assert.IsType<UnlockGrant>(grant).Unlocks).ID);
            return GrantResult.Applied;
        }

        var first = new APItemManager(SaveProgress.Bind(record, Identity, newSave: true), factory.Create);
        first.ProcessReceivedItems([Item(1)], Ready, _ => item.Name, Apply);

        var reloaded = new APItemManager(SaveProgress.Bind(record.ToList(), Identity, newSave: false), factory.Create);
        reloaded.ProcessReceivedItems([Item(1), Item(1)], Ready, _ => item.Name, Apply);
        Assert.Equal(new[] { "t_wood_small", "t_wood_big" }, stages);
        Assert.False(ItemProcessor.IsApplyingItem);
    }

    [Fact]
    public void ItemsThatArriveDuringLoadingApplyWhenReadyWithoutAnotherPacket()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var manager = CreateManager(save);
        var history = new[] { Item(1) };
        var ready = false;
        var readinessChecks = 0;
        var applications = 0;

        bool CanApply()
        {
            readinessChecks++;
            return ready;
        }

        GrantResult Apply(IItemGrant grant, Action<string> queueTrap)
        {
            applications++;
            return GrantResult.Applied;
        }

        manager.Update(null!, 0, CanApply, Name, Apply, UnexpectedError);
        Assert.Equal(0, readinessChecks);
        manager.Update(history, 0, CanApply, Name, Apply, UnexpectedError);
        Assert.Equal(0, applications);

        ready = true;
        manager.Update(history, 0.1f, CanApply, Name, Apply, UnexpectedError);
        Assert.Equal(1, applications);
        Assert.Single(save.Data.Receipts);

        var checksAfterProcessing = readinessChecks;
        manager.Update(history, 1, CanApply, Name, Apply, UnexpectedError);
        Assert.Equal(checksAfterProcessing, readinessChecks);
        Assert.Equal(1, applications);
    }

    [Fact]
    public void OnlyASafeRetryWaitsForTheRetryDelay()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var manager = CreateManager(save);
        var history = new[] { Item(1), Item(2) };
        var attempts = new List<string>();

        GrantResult Apply(IItemGrant grant, Action<string> queueTrap)
        {
            attempts.Add(Assert.IsType<TestGrant>(grant).Name);
            return attempts.Count == 1 ? GrantResult.Retry : GrantResult.Applied;
        }

        manager.Update(history, 0, Ready, Name, Apply, UnexpectedError);
        manager.Update(history, 0.5f, Ready, Name, Apply, UnexpectedError);
        Assert.Equal(new[] { "Item 1" }, attempts);
        Assert.Empty(save.Data.Receipts);

        manager.Update(history, 1, Ready, Name, Apply, UnexpectedError);
        Assert.Equal(new[] { "Item 1", "Item 1", "Item 2" }, attempts);
        Assert.Equal(2, save.Data.Receipts.Count);
    }

    [Fact]
    public void NewHistoryPublishedDuringAnUpdateStillGetsProcessed()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var manager = CreateManager(save);
        var original = new[] { Item(1), Item(2) };
        var updated = new[] { Item(1), Item(2), Item(3) };
        var current = original;
        var applied = new List<string>();

        GrantResult Apply(IItemGrant grant, Action<string> queueTrap)
        {
            applied.Add(Assert.IsType<TestGrant>(grant).Name);
            current = updated;
            return GrantResult.Applied;
        }

        manager.Update(original, 0, () => ReferenceEquals(current, original), Name, Apply, UnexpectedError);
        Assert.Equal(new[] { "Item 1" }, applied);

        manager.Update(updated, 0.1f, () => ReferenceEquals(current, updated), Name, Apply, UnexpectedError);
        Assert.Equal(new[] { "Item 1", "Item 2", "Item 3" }, applied);
    }

    private static void UnexpectedError(string message) => throw new Xunit.Sdk.XunitException(message);

    private static APItemManager CreateManager(SaveProgress progress) =>
        new(progress, (name, stage) => new TestGrant(name, stage));

    private static void Process(APItemManager manager, IReadOnlyList<NetworkItem> history, Func<bool> ready,
        Func<long, string> getName, Func<string, int, Action<string>, GrantResult> apply)
    {
        manager.ProcessReceivedItems(history, ready, getName, (grant, queueTrap) =>
        {
            var testGrant = Assert.IsType<TestGrant>(grant);
            return apply(testGrant.Name, testGrant.Stage, queueTrap);
        });
    }

    private sealed class TestGrant(string name, int stage) : IItemGrant, IDisposable
    {
        public string Name { get; } = name;
        public int Stage { get; } = stage;
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
        public GrantResult Apply(GrantContext context) => throw new InvalidOperationException();
    }

    private static bool Ready() => true;
    private static string Name(long id) => $"Item {id}";
    private static GrantResult Applied(string name, int count, Action<string> queueTrap) => GrantResult.Applied;
    private static GrantResult UnexpectedGrant(string name, int count, Action<string> queueTrap) =>
        throw new Xunit.Sdk.XunitException($"Unexpected grant '{name}'.");

    private sealed class FailingRecordList : System.Collections.ObjectModel.Collection<string>
    {
        public bool FailWrites { get; set; }

        protected override void SetItem(int index, string item)
        {
            if (FailWrites) throw new IOException("Could not write the AP record.");
            base.SetItem(index, item);
        }
    }
}
